import fs from 'node:fs';
import path from 'node:path';
import ts from 'typescript';

const sourceRoot = path.resolve('src');
const rawControls = new Set(['input', 'select', 'textarea']);
const accessibleNameProperties = new Set(['aria-label', 'aria-labelledby']);
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
  const labelledIds = new Set();

  function collectLabels(node) {
    if (ts.isJsxOpeningElement(node) && node.tagName.getText(sourceFile) === 'label') {
      const htmlFor = findAttribute(node, 'htmlFor');
      const value = associationKey(htmlFor?.initializer);
      if (value)
        labelledIds.add(value);
    }
    ts.forEachChild(node, collectLabels);
  }

  function inspectControls(node) {
    if (ts.isJsxOpeningElement(node) || ts.isJsxSelfClosingElement(node)) {
      const controlName = node.tagName.getText(sourceFile);
      if (rawControls.has(controlName) && !isHiddenInput(node) && !hasAccessibleName(node, labelledIds)) {
        const position = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile));
        destination.push(`${path.relative(process.cwd(), filePath)}:${position.line + 1} <${controlName}> requires a non-empty aria-label, aria-labelledby, explicit label association, or wrapping label.`);
      }
    }
    ts.forEachChild(node, inspectControls);
  }

  collectLabels(sourceFile);
  inspectControls(sourceFile);

  function hasAccessibleName(node, explicitLabelIds) {
    const namingAttribute = node.attributes.properties.find(property =>
      ts.isJsxAttribute(property) && accessibleNameProperties.has(property.name.getText(sourceFile)));
    if (namingAttribute && !hasEmptyLiteral(namingAttribute.initializer))
      return true;

    const id = associationKey(findAttribute(node, 'id')?.initializer);
    if (id && explicitLabelIds.has(id))
      return true;

    for (let parent = node.parent; parent; parent = parent.parent) {
      if (ts.isJsxElement(parent) && parent.openingElement.tagName.getText(sourceFile) === 'label')
        return labelHasContent(parent);
      if (ts.isJsxElement(parent) || ts.isJsxFragment(parent))
        continue;
      break;
    }

    return false;
  }

  function labelHasContent(label) {
    return label.children.some(child => {
      if (ts.isJsxText(child))
        return child.text.trim().length > 0;
      if (ts.isJsxExpression(child))
        return child.expression != null && !hasEmptyExpression(child.expression);
      return ts.isJsxElement(child) && child.openingElement.tagName.getText(sourceFile) !== 'input'
        && child.openingElement.tagName.getText(sourceFile) !== 'select'
        && child.openingElement.tagName.getText(sourceFile) !== 'textarea';
    });
  }

  function isHiddenInput(node) {
    if (node.tagName.getText(sourceFile) !== 'input')
      return false;
    return staticAttributeValue(findAttribute(node, 'type')?.initializer)?.toLowerCase() === 'hidden';
  }

  function findAttribute(node, name) {
    return node.attributes.properties.find(property =>
      ts.isJsxAttribute(property) && property.name.getText(sourceFile) === name);
  }
}

function selfTest() {
  const detected = [];
  inspectSource('raw-control-label-self-test.tsx', `
    export function Fixture() {
      const dynamicId = 'dynamic-id';
      return <>
        <input />
        <input aria-label="" />
        <select aria-labelledby={undefined} />
        <input aria-label="Named input" />
        <label>Wrapped input<input /></label>
        <label htmlFor="explicit-id">Explicit input</label><input id="explicit-id" />
        <label htmlFor={dynamicId}>Dynamic input</label><input id={dynamicId} />
        <input type="hidden" />
      </>;
    }
  `, detected);

  if (detected.length !== 3
    || detected.filter(failure => failure.includes('<input>')).length !== 2
    || detected.filter(failure => failure.includes('<select>')).length !== 1) {
    console.error(`Raw form control accessible-name self-test failed: expected three representative violations, found ${detected.length}.`);
    process.exit(1);
  }
}

function staticAttributeValue(initializer) {
  if (!initializer)
    return null;
  if (ts.isStringLiteral(initializer))
    return initializer.text.trim() || null;
  if (ts.isJsxExpression(initializer) && initializer.expression
    && (ts.isStringLiteral(initializer.expression) || ts.isNoSubstitutionTemplateLiteral(initializer.expression)))
    return initializer.expression.text.trim() || null;
  return null;
}

function associationKey(initializer) {
  return staticAttributeValue(initializer) ?? initializer?.getText().trim() ?? null;
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
  console.error('Raw production form controls must have durable accessible names:');
  for (const failure of failures)
    console.error(`- ${failure}`);
  process.exit(1);
}

console.log('Raw form control accessible-name check passed.');
