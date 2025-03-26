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
    [
      "semantic-release-replace-plugin",
      {
        "replacements": [
          {
            "files": ["package.json"],
            "from": "\"version\": \".*\"",
            "to": "\"version\": \"${nextRelease.version}\"",
            "results": [
              {
                "file": "package.json",
                "hasChanged": true,
                "numMatches": 1,
                "numReplacements": 1
              }
            ],
            "countMatches": true
          },
          {
            "files": [`${name}.csproj`],
            "from": "<Version>.*</Version>",
            "to": "<Version>${nextRelease.version}</Version>",
            "results": [
              {
                "file": `${name}.csproj`,
                "hasChanged": true,
                "numMatches": 1,
                "numReplacements": 1
              }
            ],
            "countMatches": true
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
    '@semantic-release/release-notes-generator',
    [
      '@semantic-release/changelog',
      {
        changelogFile: 'CHANGELOG.md',
      },
    ],
    [
      '@semantic-release/git',
      {
        assets: [
          'CHANGELOG.md',
          'package.json',
          '*.csproj',
        ],
        message:
          `chore(${name}): release version \${nextRelease.version} [skip ci]`,
      },
    ],
    [
      '@semantic-release/github',
    ]
  ]
};