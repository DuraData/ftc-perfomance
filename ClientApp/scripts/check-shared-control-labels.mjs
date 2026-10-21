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
  inspectSource(filePath, sourceText, failures);
}

function inspectSource(filePath, sourceText, destination) {
  const sourceFile = ts.createSourceFile(filePath, sourceText, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX);

  function visit(node) {
    if (ts.isJsxOpeningElement(node) || ts.isJsxSelfClosingElement(node)) {
      const controlName = node.tagName.getText(sourceFile);
      if (sharedControls.has(controlName)) {
        const namingAttribute = node.attributes.properties.find(property =>
          ts.isJsxAttribute(property) && accessibleNameProperties.has(property.name.getText(sourceFile)));

        if (!namingAttribute || hasEmptyLiteral(namingAttribute.initializer)) {
          const position = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile));
          destination.push(`${path.relative(process.cwd(), filePath)}:${position.line + 1} ${controlName} requires a non-empty label, aria-label, or aria-labelledby.`);
        }
      }
    }

    ts.forEachChild(node, visit);
  }

  visit(sourceFile);
}

function selfTest() {
  const detected = [];
  inspectSource('shared-control-label-self-test.tsx', `
    export function Fixture({ dynamicName }) {
      return <>
        <Input />
        <Select label="" />
        <Textarea aria-label={undefined} />
        <Input label="Named input" />
        <Select aria-label={dynamicName} />
        <Textarea aria-labelledby="description-label" />
      </>;
    }
  `, detected);

  if (detected.length !== 3
    || !detected.some(failure => failure.includes('Input requires'))
    || !detected.some(failure => failure.includes('Select requires'))
    || !detected.some(failure => failure.includes('Textarea requires'))) {
    console.error(`Shared form control accessible-name self-test failed: expected three representative violations, found ${detected.length}.`);
    process.exit(1);
  }
}

function hasEmptyLiteral(initializer) {
  if (!initializer)
    return true;

  if (ts.isStringLiteral(initializer))
    return initializer.text.trim().length === 0;

  if (!ts.isJsxExpression(initializer) || !initializer.expression)
    return false;

  return hasEmptyExpression(initializer.expression);
}

function hasEmptyExpression(expression) {
  if ((ts.isStringLiteral(expression) || ts.isNoSubstitutionTemplateLiteral(expression))
    && expression.text.trim().length === 0)
    return true;
  return expression.kind === ts.SyntaxKind.NullKeyword
    || (ts.isIdentifier(expression) && expression.text === 'undefined')
    || (ts.isVoidExpression(expression) && ts.isNumericLiteral(expression.expression) && expression.expression.text === '0');
}

selfTest();
walk(sourceRoot);

if (failures.length > 0) {
  console.error('Shared form controls must have durable accessible names:');
  for (const failure of failures)
    console.error(`- ${failure}`);
  process.exit(1);
}

console.log('Shared form control accessible-name check passed.');
