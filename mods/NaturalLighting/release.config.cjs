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
      "semantic-release-replace-plugin",
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
          }
        ]
      }
    ],
    [
      '@semantic-release/exec',
      {
        publishCmd: 'npm run publish'
      },
    ],
    [
      '@semantic-release/github',
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
