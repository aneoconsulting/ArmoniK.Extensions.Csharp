const baseURL = process.env.NODE_ENV === 'production' ? '/ArmoniK.Extensions.Csharp/' : '/'

export default defineNuxtConfig({
  extends: '@aneoconsultingfr/armonik-docs-theme',

  app: {
    baseURL: baseURL,
    head: {
      link: [
        {
          rel: 'icon',
          type: 'image/ico',
          href: `${baseURL}favicon.ico`,
        }
      ]
    }
  },

  runtimeConfig: {
    public: {
      siteName: 'ArmoniK.Extensions.Csharp',
      siteDescription: 'An extension library for C# to use ArmoniK',
    }
  },
})
