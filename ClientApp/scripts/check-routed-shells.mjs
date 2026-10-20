import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const clientRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const appPath = path.join(clientRoot, 'src', 'app.tsx');
const appSource = fs.readFileSync(appPath, 'utf8');
const lazyImportPattern = /const\s+(\w+)\s*=\s*lazy\(\s*\(\)\s*=>\s*import\(\s*(['"])([^'"]+)\2\s*\)/g;
const routedModules = new Map();
let matchedLazyImports = 0;

const resolveModulePath = moduleSpecifier => {
  const sourcePath = path.resolve(path.dirname(appPath), moduleSpecifier);
  const candidates = [`${sourcePath}.tsx`, `${sourcePath}.ts`, path.join(sourcePath, 'index.tsx'), path.join(sourcePath, 'index.ts')];
  return candidates.find(fs.existsSync) ?? candidates[0];
};

for (const match of appSource.matchAll(lazyImportPattern)) {
  const [, componentName, , moduleSpecifier] = match;
  const modulePath = resolveModulePath(moduleSpecifier);
  const names = routedModules.get(modulePath) ?? [];
  names.push(componentName);
  routedModules.set(modulePath, names);
  matchedLazyImports += 1;
}

const declaredLazyImports = appSource.match(/\blazy\s*\(/g)?.length ?? 0;
if (matchedLazyImports === 0 || matchedLazyImports !== declaredLazyImports) {
  throw new Error(`Routed-shell governance accounted for ${matchedLazyImports} of ${declaredLazyImports} lazy application imports.`);
}

const failures = [];
for (const [modulePath, componentNames] of routedModules) {
  if (!fs.existsSync(modulePath)) {
    failures.push(`${componentNames.join(', ')}: missing module ${path.relative(clientRoot, modulePath)}`);
    continue;
  }

  const source = fs.readFileSync(modulePath, 'utf8');
  const importsAppShell = /import\s*\{[^}]*\bAppShell\b[^}]*\}\s*from\s*['"][^'"]*layout\/AppShell['"]/.test(source);
  const rendersAppShell = /<AppShell(?:\s|>)/.test(source);
  if (!importsAppShell || !rendersAppShell) {
    failures.push(`${componentNames.join(', ')}: ${path.relative(clientRoot, modulePath)} must import and render AppShell`);
  }
}

if (failures.length > 0) {
  console.error('Routed workspace shell governance failed:');
  for (const failure of failures) console.error(`- ${failure}`);
  process.exit(1);
}

console.log(`Routed workspace shell governance passed: ${routedModules.size} lazy route modules import and render AppShell.`);
