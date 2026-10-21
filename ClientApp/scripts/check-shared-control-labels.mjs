import fs from 'node:fs';
import path from 'node:path';
import ts from 'typescript';

const sourceRoot = path.resolve('src');
const sharedControls = new Set(['Input', 'Select', 'Textarea']);
const accessibleNameProperties = new Set(['label', 'aria-label', 'aria-labelledby']);
const failures = [];

function walk(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      walk(fullPath);
      continue;
    }

    if (!entry.name.endsWith('.tsx') || entry.name.includes('.test.') || entry.name.includes('.spec.'))
      continue;

    inspect(fullPath);
  }
}

function inspect(filePath) {
  const sourceText = fs.readFileSync(filePath, 'utf8');
  const sourceFile = ts.createSourceFile(filePath, sourceText, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX);

  function visit(node) {
    if (ts.isJsxOpeningElement(node) || ts.isJsxSelfClosingElement(node)) {
      const controlName = node.tagName.getText(sourceFile);
      if (sharedControls.has(controlName)) {
        const namingAttribute = node.attributes.properties.find(property =>
          ts.isJsxAttribute(property) && accessibleNameProperties.has(property.name.getText(sourceFile)));

        if (!namingAttribute || hasEmptyLiteral(namingAttribute.initializer)) {
          const position = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile));
          failures.push(`${path.relative(process.cwd(), filePath)}:${position.line + 1} ${controlName} requires a non-empty label, aria-label, or aria-labelledby.`);
        }
      }
    }

    ts.forEachChild(node, visit);
  }

  visit(sourceFile);
}

function hasEmptyLiteral(initializer) {
  if (!initializer)
    return true;

  if (ts.isStringLiteral(initializer))
    return initializer.text.trim().length === 0;

  if (!ts.isJsxExpression(initializer) || !initializer.expression)
    return false;

  return (ts.isStringLiteral(initializer.expression) || ts.isNoSubstitutionTemplateLiteral(initializer.expression))
    && initializer.expression.text.trim().length === 0;
}

walk(sourceRoot);

if (failures.length > 0) {
  console.error('Shared form controls must have durable accessible names:');
  for (const failure of failures)
    console.error(`- ${failure}`);
  process.exit(1);
}

console.log('Shared form control accessible-name check passed.');
