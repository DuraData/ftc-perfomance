import js from '@eslint/js';
import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import tseslint from 'typescript-eslint';

function jsxElementName(node) {
  return node?.type === 'JSXIdentifier' ? node.name : null;
}

function staticJsxAttributeValue(attribute) {
  if (!attribute?.value) return null;
  if (attribute.value.type === 'Literal') return attribute.value.value;
  if (attribute.value.type === 'JSXExpressionContainer' && attribute.value.expression?.type === 'Literal') return attribute.value.expression.value;
  return undefined;
}

function jsxAttributeIdentity(attribute, sourceCode) {
  const value = staticJsxAttributeValue(attribute);
  if (typeof value === 'string' && value.trim()) return `literal:${value}`;
  if (attribute?.value?.type === 'JSXExpressionContainer' && attribute.value.expression?.type !== 'JSXEmptyExpression') {
    return `expression:${sourceCode.getText(attribute.value.expression)}`;
  }
  return null;
}

const localAccessibility = {
  rules: {
    'raw-control-has-accessible-name': {
      meta: {
        type: 'problem',
        docs: { description: 'Require a durable accessible name for raw form controls.' },
        schema: [],
        messages: {
          unnamed: 'Raw {{control}} controls must have aria-label, aria-labelledby, an id for explicit association, or a wrapping label.',
        },
      },
      create(context) {
        const sourceCode = context.sourceCode ?? context.getSourceCode();
        const controls = [];
        const labelledIds = new Set();

        return {
          JSXOpeningElement(node) {
            const attributes = node.attributes.filter(attribute => attribute.type === 'JSXAttribute');
            const element = jsxElementName(node.name);
            if (element === 'label') {
              const htmlFor = attributes.find(attribute => jsxElementName(attribute.name) === 'htmlFor');
              const targetId = jsxAttributeIdentity(htmlFor, sourceCode);
              if (targetId) labelledIds.add(targetId);
              return;
            }

            if (!['input', 'select', 'textarea'].includes(element)) return;
            const attributeNames = new Set(attributes.map(attribute => jsxElementName(attribute.name)));
            const typeAttribute = attributes.find(attribute => jsxElementName(attribute.name) === 'type');
            if (element === 'input' && staticJsxAttributeValue(typeAttribute) === 'hidden') return;

            const hasAccessibleNameAttribute = ['aria-label', 'aria-labelledby'].some(name => {
              if (!attributeNames.has(name)) return false;
              const value = staticJsxAttributeValue(attributes.find(attribute => jsxElementName(attribute.name) === name));
              return value === undefined || (typeof value === 'string' && value.trim().length > 0);
            });
            if (hasAccessibleNameAttribute) return;

            let ancestor = node.parent;
            while (ancestor) {
              if (ancestor.type === 'JSXElement' && jsxElementName(ancestor.openingElement?.name) === 'label') return;
              ancestor = ancestor.parent;
            }

            const idAttribute = attributes.find(attribute => jsxElementName(attribute.name) === 'id');
            controls.push({ node, control: element, id: jsxAttributeIdentity(idAttribute, sourceCode) });
          },
          'Program:exit'() {
            controls.forEach(({ node, control, id }) => {
              if (id && labelledIds.has(id)) return;
              context.report({ node, messageId: 'unnamed', data: { control } });
            });
          },
        };
      },
    },
  },
};

export default tseslint.config(
  { ignores: ['dist'] },
  {
    extends: [js.configs.recommended, ...tseslint.configs.recommended],
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
    },
    plugins: {
      local: localAccessibility,
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      // React Hooks 7 adds this advisory to the recommended preset. Existing
      // data-loading effects intentionally update local request state; retain
      // the project's previous lint contract until those flows are refactored.
      'react-hooks/set-state-in-effect': 'off',
      'react-hooks/purity': 'off',
      'local/raw-control-has-accessible-name': 'error',
      'react-refresh/only-export-components': [
        'warn',
        { allowConstantExport: true },
      ],
    },
  },
  {
    files: ['src/**/*.{ts,tsx}'],
    ignores: ['src/**/*.test.{ts,tsx}', 'src/**/*.spec.{ts,tsx}', 'src/test/**'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: [
                '**/mockData',
                '**/mockData.*',
                '**/mocks/**',
                '**/fixtures/**',
              ],
              message:
                'Production modules must load governed API data; mock and fixture modules are test-only.',
            },
          ],
        },
      ],
    },
  },
);
