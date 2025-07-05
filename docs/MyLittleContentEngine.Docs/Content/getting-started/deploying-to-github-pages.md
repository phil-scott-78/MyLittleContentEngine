---
title: "Deploying to GitHub Pages"
description: "Configure and deploy your MyLittleContentEngine site to GitHub Pages with automated builds"
order: 1090
---

This tutorial covers:

- Setting up GitHub Actions for automated builds
- Configuring base URLs for subdirectory deployment
- Handling static assets and routing correctly
- Custom domain configuration
- Troubleshooting common deployment issues

## Prerequisites

- A GitHub account and repository
- Completed at least the ["Creating Your First Site"](creating-first-site) tutorial
- Basic understanding of Git and GitHub
- Your MyLittleContentEngine project pushed to a GitHub repository

<Steps>
<Step stepNumber="1">
## Prepare Your Repository

First, ensure your project is properly configured for GitHub Pages deployment.

### Repository Settings

1. Navigate to your repository on GitHub
2. Go to **Settings** → **Pages**
3. Under **Source**, select **GitHub Actions**

### Project Structure

Make sure your project structure follows this pattern:

```
your-repo/
├── .github/
│   └── workflows/
│       └── deploy.yml
├── src/
│   └── YourProject/
│       ├── YourProject.csproj
│       ├── Program.cs
│       └── Content/
├── global.json
└── README.md
```

Note that we are using a [`global.json`](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) file to specify the
.NET SDK version. This ensures consistent builds across different environments, and is used in the GH pages to install
the appropriate .NET version.
</Step>
<Step stepNumber="2">
## Configure GitHub Actions Workflow

Create `.github/workflows/deploy.yml` in your repository root:

```yaml
name: Build and publish to GitHub Pages

on:
  push:
    branches: [ "*" ]
  pull_request:
    branches: [ "main" ]

env:
  ASPNETCORE_ENVIRONMENT: Production
  WEBAPP_PATH: ./src/YourProject/
  WEBAPP_CSPROJ: YourProject.csproj

permissions:
  contents: read
  pages: write
  id-token: write

# Allow only one concurrent deployment
concurrency:
  group: "pages"
  cancel-in-progress: false

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Install .NET
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - name: Run webapp and generate static files
        env:
          BaseUrl: "/your-repository-name/"
          DOTNET_CLI_TELEMETRY_OPTOUT: true
        run: |
          dotnet build
          dotnet run --project ${{ env.WEBAPP_PATH }}${{env.WEBAPP_CSPROJ}} --configuration Release -- build

      - name: Install minify
        run: |
          curl -sfL https://github.com/tdewolff/minify/releases/latest/download/minify_linux_amd64.tar.gz | tar -xzf - -C /tmp
          sudo mv /tmp/minify /usr/local/bin/

      - name: Minify CSS and JavaScript files
        run: |
          # Minify styles.css if it exists
          if [ -f "${{ env.WEBAPP_PATH }}output/styles.css" ]; then
            /usr/local/bin/minify -o "${{ env.WEBAPP_PATH }}output/styles.css" "${{ env.WEBAPP_PATH }}output/styles.css"
            echo "Minified styles.css"
          fi
          
          # Minify scripts.js if it exists
          if [ -f "${{ env.WEBAPP_PATH }}output/_content/MyLittleContentEngine.UI/scripts.js" ]; then
            /usr/local/bin/minify -o "${{ env.WEBAPP_PATH }}output/_content/MyLittleContentEngine.UI/scripts.js" "${{ env.WEBAPP_PATH }}output/_content/MyLittleContentEngine.UI/scripts.js"
            echo "Minified scripts.js"
          fi
          
          # Show file sizes after minification
          echo "File sizes after minification:"
          if [ -f "${{ env.WEBAPP_PATH }}output/styles.css" ]; then
            ls -lh "${{ env.WEBAPP_PATH }}output/styles.css"
          fi
          if [ -f "${{ env.WEBAPP_PATH }}output/_content/MyLittleContentEngine.UI/scripts.js" ]; then
            ls -lh "${{ env.WEBAPP_PATH }}output/_content/MyLittleContentEngine.UI/scripts.js"
          fi

      - name: Setup Pages
        uses: actions/configure-pages@v4

      - name: Add .nojekyll file
        run: touch ${{ env.WEBAPP_PATH }}output/.nojekyll

      - name: Upload artifact
        uses: actions/upload-pages-artifact@v3
        with:
          path: ${{ env.WEBAPP_PATH }}output

  deploy:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    needs: build
    if: (github.event_name == 'push' && github.ref == 'refs/heads/main') || (github.event_name == 'pull_request' && github.event.action == 'closed' && github.event.pull_request.merged == true)
    steps:
      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v4
```

### Key Configuration Points

**Environment Variables to Update:**

- `WEBAPP_PATH`: Path to your project directory
- `WEBAPP_CSPROJ`: Your project file name
- `BaseUrl`: Your repository name (for GitHub Pages subdirectory)

**Important:** Replace `your-repository-name` and `YourProject` with your actual values.
</Step>
<Step stepNumber="3">
## Configuring Base URLs

This is one of the most important aspects of GitHub Pages deployment. Understanding BaseUrl is crucial for your site to
work correctly.

### Why BaseUrl Matters

**Local Development vs GitHub Pages:**

- **Local development**: Your site runs at `http://localhost:5000/` (root domain)
- **GitHub Pages**: Your site runs at `https://username.github.io/repository-name/` (subdirectory)

Without proper BaseUrl configuration, your site will have broken links, missing CSS, and non-functional navigation when
deployed to GitHub Pages.

See the [Linking Documents and Media](/guides/linking-documents-and-media) guide for more details on how
MyLittleContentEngine handles links.

### Update Your Program.cs

Modify your `Program.cs` to handle the base URL from environment variables:

```csharp
builder.Services.AddContentEngineService(() => new ContentEngineOptions
{
    SiteTitle = "My Site",
    SiteDescription = "My site description",
    BaseUrl = Environment.GetEnvironmentVariable("BaseUrl") ?? "/",
    ContentRootPath = "Content",
});
```
</Step>
<Step stepNumber="4">
## Set Up GitHub Pages

### Enable GitHub Pages

1. Go to your repository **Settings**
2. Navigate to **Pages** in the sidebar
3. Under **Source**, select **GitHub Actions**
4. Save the settings
</Step>
<Step stepNumber="5">

## Test Your Deployment

### Push Your Changes

Commit and push your workflow file:

```bash
git add .github/workflows/deploy.yml
git commit -m "Add GitHub Pages deployment workflow"
git push origin main
```

### Monitor the Build

1. Go to the **Actions** tab in your repository
2. Watch the workflow run
3. Check for any errors in the build process

### Verify Deployment

Once the workflow completes:

1. Your site should be available at `https://username.github.io/repository-name/`
2. Check that all pages load correctly
3. Verify that navigation works properly
4. Test that images and other assets load

</Step>
<Step stepNumber="6">
## Custom Domain (Optional)

To use a custom domain:

### Configure DNS

Add a CNAME record pointing to `username.github.io`:

```
CNAME  www.yourdomain.com  username.github.io
```

### Update Repository Settings

1. Go to **Settings** → **Pages**
2. Enter your custom domain in the **Custom domain** field
3. GitHub will automatically create a `CNAME` file in your repository

### Update Base URL

Modify your workflow to use your custom domain:

```yaml
- name: Run webapp and generate static files
  env:
    BaseUrl: "/"  # Root path for custom domain
    DOTNET_CLI_TELEMETRY_OPTOUT: true
```
</Step>
</Steps>



Your MyLittleContentEngine site is now automatically deployed to GitHub Pages! Every time you push to the main branch,
your site will be rebuilt and deployed automatically.