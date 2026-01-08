export default function combinePath(basePath, paths = []) {
  const slash = '/';

  return `${basePath}${slash}${paths.join(slash)}`;
}
