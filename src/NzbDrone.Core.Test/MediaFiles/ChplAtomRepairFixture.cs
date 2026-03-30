using System;
using System.IO;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class ChplAtomRepairFixture : CoreTest
    {
        private string _tempFile;

        [TearDown]
        public void Cleanup()
        {
            if (_tempFile != null && File.Exists(_tempFile))
            {
                File.Delete(_tempFile);
            }
        }

        /// <summary>
        /// Builds a minimal MPEG-4 file structure in memory.
        /// Layout: ftyp + moov(udta(chpl(...)))
        /// </summary>
        private byte[] BuildMp4WithChpl(byte chplVersion, byte[] chplPayload)
        {
            // chpl atom: size(4) + "chpl"(4) + version(1) + flags(3) + [reserved(1) if v1] + payload
            var chplHeaderLen = 4 + 4 + 1 + 3 + (chplVersion == 1 ? 1 : 0);
            var chplSize = chplHeaderLen + chplPayload.Length;

            // udta atom: size(4) + "udta"(4) + chpl
            var udtaSize = 8 + chplSize;

            // moov atom: size(4) + "moov"(4) + udta
            var moovSize = 8 + udtaSize;

            // ftyp atom: size(4) + "ftyp"(4) + "M4B "(4) = 12
            var ftypSize = 12;

            var total = ftypSize + moovSize;
            var ms = new MemoryStream(total);
            var bw = new BinaryWriter(ms);

            // ftyp
            WriteBigEndianInt32(bw, ftypSize);
            bw.Write(Encoding.ASCII.GetBytes("ftyp"));
            bw.Write(Encoding.ASCII.GetBytes("M4B "));

            // moov
            WriteBigEndianInt32(bw, moovSize);
            bw.Write(Encoding.ASCII.GetBytes("moov"));

            // udta
            WriteBigEndianInt32(bw, udtaSize);
            bw.Write(Encoding.ASCII.GetBytes("udta"));

            // chpl
            WriteBigEndianInt32(bw, chplSize);
            bw.Write(Encoding.ASCII.GetBytes("chpl"));
            bw.Write(chplVersion);           // version
            bw.Write(new byte[] { 0, 0, 0 }); // flags
            if (chplVersion == 1)
            {
                bw.Write((byte)0x00);          // reserved byte
            }

            bw.Write(chplPayload);

            return ms.ToArray();
        }

        private static void WriteBigEndianInt32(BinaryWriter writer, int value)
        {
            writer.Write((byte)((value >> 24) & 0xFF));
            writer.Write((byte)((value >> 16) & 0xFF));
            writer.Write((byte)((value >> 8) & 0xFF));
            writer.Write((byte)(value & 0xFF));
        }

        private string WriteTempFile(byte[] data)
        {
            _tempFile = Path.Combine(Path.GetTempPath(), $"chpltest_{Guid.NewGuid()}.m4b");
            File.WriteAllBytes(_tempFile, data);
            return _tempFile;
        }

        [Test]
        public void FindNestedAtom_should_locate_chpl_inside_moov_udta()
        {
            var chplPayload = new byte[] { 0x00, 0x00, 0x00, 0x01 }; // 1 chapter
            var data = BuildMp4WithChpl(0x01, chplPayload);

            using (var ms = new MemoryStream(data))
            {
                var offset = AudioTag.FindNestedAtom(ms, 0, ms.Length, "moov", "udta", "chpl");
                offset.Should().BeGreaterOrEqualTo(0);

                // Verify the atom type at the found offset
                ms.Seek(offset + 4, SeekOrigin.Begin);
                var typeBytes = new byte[4];
                ms.Read(typeBytes, 0, 4);
                Encoding.ASCII.GetString(typeBytes).Should().Be("chpl");
            }
        }

        [Test]
        public void FindNestedAtom_should_return_negative_for_missing_atom()
        {
            var chplPayload = new byte[] { 0x00, 0x00, 0x00, 0x01 };
            var data = BuildMp4WithChpl(0x01, chplPayload);

            using (var ms = new MemoryStream(data))
            {
                var offset = AudioTag.FindNestedAtom(ms, 0, ms.Length, "moov", "udta", "hdlr");
                offset.Should().Be(-1);
            }
        }

        [Test]
        public void RepairChplAtom_should_neutralize_version_0_chpl()
        {
            // Simulate what TagLib writes: version 0 chpl (corrupted from version 1)
            var chplPayload = new byte[] { 0x00, 0x00, 0x00, 0x02 }; // 2 chapters
            var data = BuildMp4WithChpl(0x00, chplPayload);
            var path = WriteTempFile(data);

            AudioTag.RepairChplAtom(path);

            // Read back and verify "chpl" was replaced with "free"
            var repaired = File.ReadAllBytes(path);
            using (var ms = new MemoryStream(repaired))
            {
                // chpl was inside moov/udta, find udta contents start
                var chplOffset = AudioTag.FindNestedAtom(ms, 0, ms.Length, "moov", "udta", "chpl");
                chplOffset.Should().Be(-1, "chpl should no longer exist");

                var freeOffset = AudioTag.FindNestedAtom(ms, 0, ms.Length, "moov", "udta", "free");
                freeOffset.Should().BeGreaterOrEqualTo(0, "free atom should exist in place of chpl");
            }
        }

        [Test]
        public void RepairChplAtom_should_not_touch_version_1_chpl()
        {
            // Version 1 chpl is valid and should not be modified
            var chplPayload = new byte[] { 0x00, 0x00, 0x00, 0x02 }; // 2 chapters
            var data = BuildMp4WithChpl(0x01, chplPayload);
            var path = WriteTempFile(data);

            var originalBytes = File.ReadAllBytes(path);

            AudioTag.RepairChplAtom(path);

            var afterRepair = File.ReadAllBytes(path);
            afterRepair.Should().BeEquivalentTo(originalBytes, "version 1 chpl should be left unchanged");
        }

        [Test]
        public void RepairChplAtom_should_handle_file_without_chpl()
        {
            // Minimal MP4 with just ftyp + moov(udta()) but no chpl
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            // ftyp
            WriteBigEndianInt32(bw, 12);
            bw.Write(Encoding.ASCII.GetBytes("ftyp"));
            bw.Write(Encoding.ASCII.GetBytes("M4B "));

            // moov with empty udta
            var udtaSize = 8;
            var moovSize = 8 + udtaSize;
            WriteBigEndianInt32(bw, moovSize);
            bw.Write(Encoding.ASCII.GetBytes("moov"));
            WriteBigEndianInt32(bw, udtaSize);
            bw.Write(Encoding.ASCII.GetBytes("udta"));

            var data = ms.ToArray();
            var path = WriteTempFile(data);

            var originalBytes = File.ReadAllBytes(path);

            // Should not throw and file should be unchanged
            AudioTag.RepairChplAtom(path);

            var afterRepair = File.ReadAllBytes(path);
            afterRepair.Should().BeEquivalentTo(originalBytes);
        }

        [Test]
        public void RepairChplAtom_should_handle_missing_file()
        {
            // Should not throw for a non-existent file
            AudioTag.RepairChplAtom(Path.Combine(Path.GetTempPath(), "nonexistent_file.m4b"));
        }
    }
}
