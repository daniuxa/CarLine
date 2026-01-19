import importPlugin from 'eslint-plugin-import';
import reactHooks from 'eslint-plugin-react-hooks';
import { defineConfig } from 'eslint/config';

export default defineConfig({
  ignores: ['dist'],
  files: ['**/*.{js,jsx}'],
  plugins: {
    'react-hooks': reactHooks,
    import: importPlugin,
  },
  languageOptions: {
    ecmaVersion: 2021,
    sourceType: 'module',
    parserOptions: {
      ecmaVersion: 'latest',
      ecmaFeatures: { jsx: true },
    },
  },
  settings: {
    react: { version: 'detect' },
    'import/resolver': {
      node: {
        extensions: ['.js', '.jsx', '.json'],
        moduleDirectory: ['node_modules', 'src'],
      },
    },
  },
  rules: {
    'no-unused-vars': ['warn', { varsIgnorePattern: '^[A-Z_]' }],
    // Require exhaustive-deps for React hooks
    'react-hooks/exhaustive-deps': 'warn',
    // Warn about unresolved imports
    'import/no-unresolved': 'warn',
  },
})
