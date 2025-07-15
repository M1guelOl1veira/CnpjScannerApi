import { Project, SyntaxKind, Node } from "ts-morph";
import * as path from "path";
import * as fs from "fs";

interface VariableMatch {
  filePath: string;
  lineNumber: number;
  declaration: string;
  looksLikeCnpj: boolean;
  type: string;
}

const CNPJ_REGEX = /\d{2}\.?\d{3}\.?\d{3}\/\d{4}-\d{2}/;
const KEYWORDS = ["cnpj"];
const IGNORED_DIRS = ["node_modules", ".angular", ".vscode"];

export function analyzeTypeScriptProject(rootDir: string): VariableMatch[] {
  console.error("Analyzing directory:", rootDir);
  const matches: VariableMatch[] = [];
  const project = new Project({
    useInMemoryFileSystem: false,
    compilerOptions: {
      allowJs: true,
      checkJs: false,
    },
  });

  // Collect .ts and .js files
  const files = getAllFiles(rootDir, [".ts", ".js"]);
  console.error("Found source files:", files.length);

  files.forEach((filePath) => {
    console.error("Adding source file:", filePath);
    project.addSourceFileAtPath(filePath);
  });

  project.getSourceFiles().forEach((sourceFile) => {
    console.error("Processing file:", sourceFile.getFilePath());
    const fileMatches: VariableMatch[] = [];

    sourceFile.forEachDescendant((node) => {
      const kind = node.getKind();
      let match: VariableMatch | null = null;

      // Handle variable declarations
      if (kind === SyntaxKind.VariableDeclaration) {
        const declaration = node.asKindOrThrow(SyntaxKind.VariableDeclaration);
        const name = declaration.getName();
        const initializer = declaration.getInitializer();
        const valueText = initializer?.getText() || "";
        const lineNumber = declaration.getStartLineNumber();
        const type = declaration.getType().getText();
        const looksLikeCnpj = isCnpjLike(name, valueText);

        if (!["string", "number"].includes(type)) return;

        match = {
          filePath: sourceFile.getFilePath(),
          lineNumber,
          declaration: declaration.getText(),
          looksLikeCnpj,
          type,
        };
      }

      // Property declarations
      if (kind === SyntaxKind.PropertyDeclaration) {
        const prop = node.asKind(SyntaxKind.PropertyDeclaration);
        if (!prop) return;
        const name = prop.getName();
        const initializer = prop.getInitializer();
        const valueText = initializer?.getText() || "";
        const lineNumber = prop.getStartLineNumber();
        const type = prop.getType().getText();
        const looksLikeCnpj = isCnpjLike(name, valueText);

        if (!["string", "number"].includes(type)) return;

        match = {
          filePath: sourceFile.getFilePath(),
          lineNumber,
          declaration: prop.getText(),
          looksLikeCnpj,
          type,
        };
      }

      // Enum members
      if (kind === SyntaxKind.EnumMember) {
        const member = node.asKind(SyntaxKind.EnumMember);
        const initializer = member?.getInitializer();
        if (!member || !initializer) return;

        const kind = initializer.getKind();
        if (kind !== SyntaxKind.StringLiteral && kind !== SyntaxKind.NumericLiteral) return;

        const type = kind === SyntaxKind.StringLiteral ? "string" : "number";
        const name = member.getName();
        const valueText = initializer.getText();
        const lineNumber = member.getStartLineNumber();
        const looksLikeCnpj = isCnpjLike(name, valueText);

        match = {
          filePath: sourceFile.getFilePath(),
          lineNumber,
          declaration: member.getText(),
          looksLikeCnpj,
          type,
        };
      }

      // Get accessors
      if (kind === SyntaxKind.GetAccessor) {
        const getter = node.asKind(SyntaxKind.GetAccessor);
        if (!getter) return;
        const returnType = getter.getReturnType().getText();
        if (!["string", "number"].includes(returnType)) return;

        const name = getter.getName();
        const lineNumber = getter.getStartLineNumber();
        const looksLikeCnpj = isCnpjLike(name);

        match = {
          filePath: sourceFile.getFilePath(),
          lineNumber,
          declaration: getter.getText(),
          looksLikeCnpj,
          type: returnType,
        };
      }

      // Property signatures
      if (kind === SyntaxKind.PropertySignature) {
        const prop = node.asKind(SyntaxKind.PropertySignature);
        if (!prop) return;
        const type = prop.getType().getText();
        if (!["string", "number"].includes(type)) return;

        const name = prop.getName();
        const lineNumber = prop.getStartLineNumber();
        const looksLikeCnpj = isCnpjLike(name);

        match = {
          filePath: sourceFile.getFilePath(),
          lineNumber,
          declaration: prop.getText(),
          looksLikeCnpj,
          type,
        };
      }

      // Function parameters
      if (kind === SyntaxKind.Parameter) {
        const param = node.asKind(SyntaxKind.Parameter);
        if (!param) return;
        const paramType = param.getType().getText();
        if (!["string", "number"].includes(paramType)) return;

        const name = param.getName();
        const lineNumber = param.getStartLineNumber();
        const looksLikeCnpj = isCnpjLike(name);

        match = {
          filePath: sourceFile.getFilePath(),
          lineNumber,
          declaration: param.getText(),
          looksLikeCnpj,
          type: paramType,
        };
      }

      if (match) fileMatches.push(match);
    });

    matches.push(...fileMatches);
  });

  return matches;
}

function getAllFiles(dir: string, exts: string[], files: string[] = []): string[] {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    const res = path.resolve(dir, entry.name);
    if (entry.isDirectory()) {
      const isIgnored = IGNORED_DIRS.some((d) =>
        res.includes(path.sep + d + path.sep) || res.endsWith(path.sep + d)
      );
      if (!isIgnored) getAllFiles(res, exts, files);
    } else if (exts.some((ext) => res.endsWith(ext))) {
      files.push(res);
    }
  }
  return files;
}

function isCnpjLike(name: string, value: string = ""): boolean {
  return (
    CNPJ_REGEX.test(value) ||
    KEYWORDS.some((k) => name.toLowerCase().includes(k))
  );
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
