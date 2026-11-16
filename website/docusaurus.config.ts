import path from 'node:path';
import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

// docs/index.md and docs/README.md both default to the "/" slug (Docusaurus
// treats index.md and README.md as interchangeable folder-index aliases),
// which collides. Without editing the Markdown, give the root README.md an
// explicit slug so it is still reachable at /project-readme.
const rootReadmePath = path.resolve(__dirname, '../docs/README.md');

const config: Config = {
  title: 'CritterStack.TestHarness',
  tagline: 'A fluent Given/When/Then test harness for Marten and Wolverine services',
  favicon: 'img/favicon.ico',

  url: 'https://ryandanthony.github.io',
  baseUrl: '/critterstack-fluent-testharness/',
  organizationName: 'ryandanthony',
  projectName: 'critterstack-fluent-testharness',
  trailingSlash: false,

  onBrokenLinks: 'throw',
  onBrokenAnchors: 'throw',

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  markdown: {
    format: 'detect',
    mermaid: true,
    hooks: {
      onBrokenMarkdownLinks: 'throw',
    },
    parseFrontMatter: async (params) => {
      const result = await params.defaultParseFrontMatter(params);
      if (path.resolve(params.filePath) === rootReadmePath) {
        result.frontMatter.slug = '/project-readme';
      }
      return result;
    },
  },

  themes: ['@docusaurus/theme-mermaid'],

  presets: [
    [
      'classic',
      {
        docs: {
          path: '../docs',
          routeBasePath: '/',
          sidebarPath: './sidebars.ts',
          // docs live outside website/, so the default editUrl + relative path would contain "../docs".
          editUrl: ({docPath}) =>
            `https://github.com/ryandanthony/critterstack-fluent-testharness/edit/main/docs/${docPath}`,
          exclude: ['**/node_modules/**'],
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    image: 'img/favicon.ico',
    colorMode: {
      defaultMode: 'dark',
      disableSwitch: false,
      respectPrefersColorScheme: false,
    },
    navbar: {
      title: 'CritterStack.TestHarness',
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docs',
          position: 'left',
          label: 'Docs',
        },
        {
          href: 'https://github.com/ryandanthony/critterstack-fluent-testharness',
          label: 'GitHub',
          position: 'right',
        },
        {
          href: 'https://www.nuget.org/packages/CritterStack.TestHarness',
          label: 'NuGet',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Documentation',
          items: [
            {
              label: 'Tutorial',
              to: '/tutorials/first-scenario',
            },
            {
              label: 'How-to guides',
              to: '/how-to/add-to-a-service',
            },
            {
              label: 'API reference',
              to: '/reference/api',
            },
          ],
        },
        {
          title: 'Community',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/ryandanthony/critterstack-fluent-testharness',
            },
            {
              label: 'NuGet',
              href: 'https://www.nuget.org/packages/CritterStack.TestHarness',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Ryan Anthony. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
