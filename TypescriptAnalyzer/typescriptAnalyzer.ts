import { Project, SyntaxKind, Node } from 'ts-morph';
import * as path from 'path';
import * as fs from 'fs';

interface VariableMatch {
  filePath: string;
  lineNumber: number;
  declaration: string;
  looksLikeCnpj: boolean;
  type: string;
}

const CNPJ_REGEX = /\d{2}\.?\d{3}\.?\d{3}\/\d{4}-\d{2}/;
const KEYWORDS = ['cnpj'];
const IGNORED_DIRS = ['node_modules', '.angular', '.vscode'];

export function analyzeTypeScriptProject(rootDir: string): VariableMatch[] {
  console.error("Analyzing directory:", rootDir);
  const matches: VariableMatch[] = [];
  const project = new Project({ useInMemoryFileSystem: false });

  const tsFiles = getAllFiles(rootDir, '.ts');
  console.error("Found .ts files:", tsFiles.length);

  tsFiles.forEach((filePath) => {
    console.error("Adding source file:", filePath);
    project.addSourceFileAtPath(filePath);
  });

  project.getSourceFiles().forEach((sourceFile) => {
    console.error("Processing file:", sourceFile.getFilePath());
    let counter = 0;
    sourceFile.forEachDescendant(() => counter++);
    console.error("AST node count for file:", counter);

    const fileMatches: VariableMatch[] = [];

    sourceFile.forEachDescendant((node) => {
      const kind = node.getKind();
      let match: VariableMatch | null = null;

      if (kind === SyntaxKind.VariableDeclaration) {
        const declaration = node.asKind(SyntaxKind.VariableDeclaration);
        if (!declaration || !isDefinitelyNumberOrString(declaration)) return;
        const name = declaration.getName();
        const initializer = declaration.getInitializer();
        const lineNumber = declaration.getStartLineNumber();
        const valueText = initializer?.getText() || '';
        const looksLikeCnpj = isCnpjLike(name, valueText);
        const type = declaration.getType().getText();

        match = {
          filePath: sourceFile.getFilePath(),
          lineNumber,
          declaration: declaration.getText(),
          looksLikeCnpj,
          type
        };
      }

      if (kind === SyntaxKind.PropertyDeclaration) {
        const prop = node.asKind(SyntaxKind.PropertyDeclaration);
        if (!prop || !isDefinitelyNumberOrString(prop)) return;
        const name = prop.getName();
        const valueText = prop.getInitializer()?.getText() || '';
        const lineNumber = prop.getStartLineNumber();
        const looksLikeCnpj = isCnpjLike(name, valueText);
        const type = prop.getType().getText();

        match = {
          filePath: sourceFile.getFilePath(),
          lineNumber,
          declaration: prop.getText(),
          looksLikeCnpj,
          type
        };
      }

      if (kind === SyntaxKind.GetAccessor) {
        const getter = node.asKind(SyntaxKind.GetAccessor);
        if (!getter || getter.getReturnType().getText() !== 'number') return;
        const name = getter.getName();
        const lineNumber = getter.getStartLineNumber();
        const looksLikeCnpj = isCnpjLike(name);
        const type = getter.getReturnType().getText();

        match = {
          filePath: sourceFile.getFilePath(),
          lineNumber,
          declaration: getter.getText(),
          looksLikeCnpj,
          type
        };
      }

      if (kind === SyntaxKind.PropertySignature) {
        const prop = node.asKind(SyntaxKind.PropertySignature);
        if (!prop || prop.getType().getText() !== 'number') return;
        const name = prop.getName();
        const lineNumber = prop.getStartLineNumber();
        const looksLikeCnpj = isCnpjLike(name);
        const type = prop.getType().getText();

        match = {
          filePath: sourceFile.getFilePath(),
          lineNumber,
          declaration: prop.getText(),
          looksLikeCnpj,
          type
        };
      }

      if (kind === SyntaxKind.EnumMember) {
        const member = node.asKind(SyntaxKind.EnumMember);
        const initializer = member?.getInitializer();
        if (!member || !initializer || initializer.getKind() !== SyntaxKind.NumericLiteral) return;
        const name = member.getName();
        const valueText = initializer.getText();
        const lineNumber = member.getStartLineNumber();
        const looksLikeCnpj = isCnpjLike(name, valueText);

        match = {
          filePath: sourceFile.getFilePath(),
          lineNumber,
          declaration: member.getText(),
          looksLikeCnpj,
          type: 'number'
        };
      }

      if (kind === SyntaxKind.Parameter) {
        const param = node.asKind(SyntaxKind.Parameter);
        if (!param || param.getType()?.getText() !== 'number') return;
        const name = param.getName();
        const lineNumber = param.getStartLineNumber();
        const looksLikeCnpj = isCnpjLike(name);
        const type = param.getType()?.getText() || 'unknown';

        match = {
          filePath: sourceFile.getFilePath(),
          lineNumber,
          declaration: param.getText(),
          looksLikeCnpj,
          type
        };
      }

      if (match) {
        console.error("Match found:", {
          file: match.filePath,
          line: match.lineNumber,
          text: match.declaration
        });
        fileMatches.push(match);
      }
    });

    matches.push(...fileMatches);
  });

  console.error("Total matches found:", matches.length);
  return matches;
}

function getAllFiles(dir: string, ext: string, files: string[] = []): string[] {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    const res = path.resolve(dir, entry.name);
    if (entry.isDirectory()) {
      const isIgnored = IGNORED_DIRS.some(d => res.includes(path.sep + d + path.sep) || res.endsWith(path.sep + d));
      if (!isIgnored) {
        getAllFiles(res, ext, files);
      }
    } else if (res.endsWith(ext)) {
      files.push(res);
    }
  }
  return files;
}

function isCnpjLike(name: string, value: string = ''): boolean {
  return CNPJ_REGEX.test(value) || KEYWORDS.some((k) => name.toLowerCase().includes(k));
}

function isDefinitelyNumberOrString(node: Node): boolean {
  const typeNode = (node as any).getTypeNode?.();
  const initializer = (node as any).getInitializer?.();

  if (typeNode && (typeNode.getText() === "number" || typeNode.getText() === "string")) return true;
  if (initializer && (initializer.getKind() === SyntaxKind.NumericLiteral || initializer.getKind() === SyntaxKind.StringLiteral)) return true;

  return false;
}

if (require.main === module) {
  const rootDir = process.argv[2];

  if (!rootDir) {
    console.error("❌ Please provide a path as an argument.");
    process.exit(1);
  }

  try {
    const result = analyzeTypeScriptProject(rootDir);
    console.log(JSON.stringify(result, null, 2));
  } catch (err) {
    console.error("❌ Error in analyzer:", err);
    console.log("[]");
    process.exit(1);
  }
}

