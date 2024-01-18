/* eslint env node */
const { name } = require('./package.json')

/** @type import('semantic-release').GlobalConfig */
module.exports = {
  branches:
    [
      "+([0-9])?(.{+([0-9]),x}).x",
      "main",
      "next",
      "next-major",
      { name: "beta", prerelease: true },
      { name: "alpha", prerelease: true },
    ],
  plugins: [
    '@semantic-release/commit-analyzer',
    '@semantic-release/release-notes-generator',
    [
      '@semantic-release/changelog',
      {
        changelogFile: 'CHANGELOG.md',
      },
    ],
    [
      "@google/semantic-release-replace-plugin",
      {
        "replacements": [
          {
            "files": ["package.json"],
            "from": "\"version\": \".*\"",
            "to": "\"version\": \"${nextRelease.version}\""
          },
          {
            "files": [`${name}.csproj`],
            "from": "<Version>.*</Version>",
            "to": "<Version>${nextRelease.version}</Version>"
          },
          {
            "files": [`${name}.csproj`],
            "from": "<PackageVersion>.*</PackageVersion>",
            "to": "<PackageVersion>${nextRelease.version}</PackageVersion>"
          },
          {
            "files": [`${name}.csproj`],
            "from": "<AssemblyVersion>.*</AssemblyVersion>",
            "to": "<AssemblyVersion>${nextRelease.version}</AssemblyVersion>"
          },
          {
            "files": [`${name}.csproj`],
            "from": "<FileVersion>.*</FileVersion>",
            "to": "<FileVersion>${nextRelease.version}</FileVersion>"
          }
        ]
      }
    ],
    [
      '@semantic-release/exec',
      {
        publishCmd: 'rimraf ./publish && npm run publish'
      },
    ],
    [
      '@semantic-release/exec',
      {
        publishCmd: '7z a -tzip ' + name + '-v${nextRelease.version}.zip *',
        execCwd: './publish'
      },
    ],
    [
      '@semantic-release/github',
      {
        assets: './publish/**/*.zip'
      },
    ],
    [
      '@semantic-release/git',
      {
        assets: [
          'CHANGELOG.md',
          'package.json',
          `${name}.csproj`,
        ],
        message:
          `chore(${name}): release version \${nextRelease.version} [skip ci]`,
      },
    ],
  ]
};
