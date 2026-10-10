import js from '@eslint/js';
import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import tseslint from 'typescript-eslint';

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
