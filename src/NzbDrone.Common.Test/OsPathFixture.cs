using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Test.Common;

namespace NzbDrone.Common.Test
{
    public class OsPathFixture : TestBase
    {
        [TestCase(@"relative", OsPathKind.Unknown)]
        [TestCase("/rooted/linux/path/", OsPathKind.Unix)]
        [TestCase("/rooted/linux/path", OsPathKind.Unix)]
        [TestCase("/", OsPathKind.Unix)]
        [TestCase("linux/path", OsPathKind.Unix)]
        [TestCase(@"Castle:unrooted+linux+path", OsPathKind.Unknown)]
        public void should_auto_detect_kind(string path, OsPathKind kind)
        {
            var result = new OsPath(path);

            result.Kind.Should().Be(kind);

            if (kind == OsPathKind.Unix)
            {
                result.IsUnixPath.Should().BeTrue();
            }
            else
            {
                result.IsUnixPath.Should().BeFalse();
            }
        }

        [Test]
        public void should_add_directory_slash()
        {
            var osPath = new OsPath(@"/rooted/linux/path/");

            osPath.Directory.Should().NotBeNull();
            osPath.Directory.ToString().Should().Be(@"/rooted/linux/");
        }

        [TestCase("/rooted/linux/path", "/rooted/linux/")]
        [TestCase("/rooted", "/")]
        [TestCase("/", null)]
        public void should_return_parent_directory(string path, string expectedParent)
        {
            var osPath = new OsPath(path);

            osPath.Directory.Should().NotBeNull();
            osPath.Directory.Should().Be(new OsPath(expectedParent));
        }

        [TestCase("/rooted/linux/path")]
        [TestCase("/")]
        public void should_detect_rooted_ospaths(string path)
        {
            var osPath = new OsPath(path);

            osPath.IsRooted.Should().BeTrue();
        }

        [TestCase(@"path")]
        [TestCase("linux/path")]
        [TestCase(@"Castle:unrooted+linux+path")]
        [TestCase(@"C:unrooted+linux+path")]
        public void should_detect_unrooted_ospaths(string path)
        {
            var osPath = new OsPath(path);

            osPath.IsRooted.Should().BeFalse();
        }

        [TestCase("/rooted/linux/path", "path")]
        [TestCase("/", null)]
        [TestCase(@"path", "path")]
        [TestCase("linux/path", "path")]
        public void should_return_filename(string path, string expectedFilePath)
        {
            var osPath = new OsPath(path);

            osPath.FileName.Should().Be(expectedFilePath);
        }

        [Test]
        public void should_compare_unix_ospathkind_case_sensitive()
        {
            var left = new OsPath(@"/rooted/Linux/path");
            var right = new OsPath(@"/rooted/linux/path");

            left.Should().NotBe(right);
        }

        [Test]
        public void should_not_ignore_trailing_slash_during_compare()
        {
            var left = new OsPath(@"/rooted/linux/path/");
            var right = new OsPath(@"/rooted/linux/path");

            left.Should().NotBe(right);
        }

        [TestCase(@"/Test", @"sub", @"/Test/sub")]
        [TestCase(@"/Test", @"sub/", @"/Test/sub/")]
        [TestCase(@"/Test/", @"sub/test/", @"/Test/sub/test/")]
        [TestCase(@"/Test/", @"/Test2/", @"/Test2/")]
        [TestCase(@"/Test", "", @"/Test")]
        public void should_combine_path(string left, string right, string expectedResult)
        {
            var osPathLeft = new OsPath(left);
            var osPathRight = new OsPath(right);

            var result = osPathLeft + osPathRight;

            result.FullPath.Should().Be(expectedResult);
        }

        [Test]
        public void should_fix_double_slashes_unix()
        {
            var osPath = new OsPath(@"/just/a//test////to/verify the/slashes/");

            osPath.Kind.Should().Be(OsPathKind.Unix);
            osPath.FullPath.Should().Be(@"/just/a/test/to/verify the/slashes/");
        }

        [TestCase(@"/parent/folder", @"/parent/folder/Sub/Folder", @"Sub/Folder")]
        public void should_create_relative_path(string parent, string child, string expected)
        {
            var left = new OsPath(child);
            var right = new OsPath(parent);

            var osPath = left - right;

            osPath.Kind.Should().Be(OsPathKind.Unknown);
            osPath.FullPath.Should().Be(expected);
        }

        [Test]
        public void should_parse_null_as_empty()
        {
            var result = new OsPath(null);

            result.FullPath.Should().BeEmpty();
            result.IsEmpty.Should().BeTrue();
        }

        [TestCase(@"/test/data", @"/test/data", true)]
        [TestCase(@"/test/data", @"/test/data/contains", true)]
        [TestCase(@"/test/data", @"/test/other", false)]
        public void should_evaluate_contains(string parent, string child, bool expectedResult)
        {
            var left = new OsPath(parent);
            var right = new OsPath(child);

            var result = left.Contains(right);

            result.Should().Be(expectedResult);
        }
    }
}
