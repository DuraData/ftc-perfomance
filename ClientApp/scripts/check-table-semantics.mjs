import fs from 'node:fs';
import path from 'node:path';
import ts from 'typescript';

const sourceRoot = path.resolve('src');
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
    inspectSource(fullPath, fs.readFileSync(fullPath, 'utf8'), failures);
  }
}

function inspectSource(filePath, sourceText, destination) {
  const sourceFile = ts.createSourceFile(filePath, sourceText, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX);

  function visit(node) {
    if (ts.isJsxOpeningElement(node) || ts.isJsxSelfClosingElement(node)) {
      const tagName = node.tagName.getText(sourceFile);
      if (tagName === 'table' && !hasTableName(node))
        addFailure(node, '<table> requires a non-empty aria-label, aria-labelledby, or caption.');
      if (tagName === 'th' && !hasValidScope(node))
        addFailure(node, '<th> requires a static scope="col" or scope="row".');
    }
    ts.forEachChild(node, visit);
  }

  function hasTableName(node) {
    for (const name of ['aria-label', 'aria-labelledby']) {
      const attribute = findAttribute(node, name);
      if (attribute && !hasEmptyValue(attribute.initializer))
        return true;
    }

    const tableElement = ts.isJsxOpeningElement(node) && ts.isJsxElement(node.parent) ? node.parent : null;
    return tableElement?.children.some(child =>
      ts.isJsxElement(child)
      && child.openingElement.tagName.getText(sourceFile) === 'caption'
      && elementHasContent(child)) ?? false;
  }

  function hasValidScope(node) {
    const scope = staticValue(findAttribute(node, 'scope')?.initializer)?.toLowerCase();
    return scope === 'col' || scope === 'row';
  }

  function elementHasContent(element) {
    return element.children.some(child => {
      if (ts.isJsxText(child))
        return child.text.trim().length > 0;
      if (ts.isJsxExpression(child))
        return child.expression != null && !isStaticallyEmptyExpression(child.expression);
      return ts.isJsxElement(child) || ts.isJsxSelfClosingElement(child);
    });
  }

  function findAttribute(node, name) {
    return node.attributes.properties.find(property =>
      ts.isJsxAttribute(property) && property.name.getText(sourceFile) === name);
  }

  function addFailure(node, message) {
    const position = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile));
    destination.push(`${path.relative(process.cwd(), filePath)}:${position.line + 1} ${message}`);
  }

  visit(sourceFile);
}

function selfTest() {
  const detected = [];
  inspectSource('table-semantics-self-test.tsx', `
    export function Fixture({ dynamicName }) {
      return <>
        <table><thead><tr><th>Missing everything</th></tr></thead></table>
        <table aria-label=""><thead><tr><th scope="cell">Bad scope</th></tr></thead></table>
        <table aria-label="Named"><thead><tr><th scope="col">Column</th></tr></thead></table>
        <table aria-labelledby={dynamicName}><tbody><tr><th scope="row">Row</th></tr></tbody></table>
        <table><caption>Captioned table</caption><tbody><tr><td>Value</td></tr></tbody></table>
      </>;
    }
  `, detected);

  if (detected.length !== 4
    || detected.filter(failure => failure.includes('<table>')).length !== 2
    || detected.filter(failure => failure.includes('<th>')).length !== 2) {
    console.error(`Table semantics self-test failed: expected four representative violations, found ${detected.length}.`);
    process.exit(1);
  }
}

function staticValue(initializer) {
  if (!initializer)
    return null;
  if (ts.isStringLiteral(initializer))
    return initializer.text.trim() || null;
  if (ts.isJsxExpression(initializer) && initializer.expression
    && (ts.isStringLiteral(initializer.expression) || ts.isNoSubstitutionTemplateLiteral(initializer.expression)))
    return initializer.expression.text.trim() || null;
  return null;
}

function hasEmptyValue(initializer) {
  if (!initializer)
    return true;
  if (ts.isStringLiteral(initializer))
    return initializer.text.trim().length === 0;
  if (!ts.isJsxExpression(initializer) || !initializer.expression)
    return false;
  return isStaticallyEmptyExpression(initializer.expression);
}

function isStaticallyEmptyExpression(expression) {
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
  console.error('Production tables must have durable names and scoped headers:');
  for (const failure of failures)
    console.error(`- ${failure}`);
  process.exit(1);
}

console.log('Production table semantics check passed.');
