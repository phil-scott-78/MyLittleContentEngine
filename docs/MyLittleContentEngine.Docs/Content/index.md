---
title: "Welcome"
description: "An opinionated and inflexible static content generator, written in .NET."
uid: "docs.index"
order: 1
---

An opinionated and inflexible static content generator, written in .NET.

* No JSON or YAML configuration files
* No Node.js or JavaScript build dependencies
* Fast. This site requires less than 25 kb of JavaScript and CSS, combined.
* Use Blazor for your layouts
* Generate compact CSS at build time from only the styles used
* Written with `dotnet watch` in mind, see changes immediately as you edit your markdown files
* Easy publishing to GitHub Pages, Azure Static Web Apps, or any other static hosting service

## Getting Started

Learn how to set up your first content site with MyLittleContentEngine.


<CardGrid>
<LinkCard Title="Creating First Site" href="xref:docs.getting-started.creating-first-site" >
<Icon>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="2em" height="2em" stroke="currentColor">
    <path d="M8 4.5V3M16 4.5V3" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
    <path d="M19.5 8L21 8M19.5 16H21" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
    <path d="M8 21V19.5M16 21V19.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
    <path d="M3 8L4.5 8M3 16H4.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
    <path d="M8 11C8 9.58579 8 8.87868 8.43934 8.43934C8.87868 8 9.58579 8 11 8H13C14.4142 8 15.1213 8 15.5607 8.43934C16 8.87868 16 9.58579 16 11V13C16 14.4142 16 15.1213 15.5607 15.5607C15.1213 16 14.4142 16 13 16H11C9.58579 16 8.87868 16 8.43934 15.5607C8 15.1213 8 14.4142 8 13V11Z" stroke="currentColor" stroke-width="1.5"></path>
</svg>
</Icon>
Build a complete content site from scratch using MyLittleContentEngine
</LinkCard>

<LinkCard Title="Connecting to Roslyn" href="xref:docs.getting-started.connecting-to-roslyn" >
<Icon>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="2em" height="2em">
    <path d="M4.51255 19.4866C7.02498 21.8794 10.016 20.9223 11.2124 19.9532C11.8314 19.4518 12.1097 19.1277 12.3489 18.8884C13.1864 18.1107 13.1326 17.3331 12.5882 16.711C12.3704 16.462 10.9731 15.1198 9.63313 13.7439C8.93922 13.0499 8.46066 12.5595 8.05149 12.1647C7.50354 11.6185 7.02499 10.9922 6.30715 11.0101C5.64913 11.0101 5.17057 11.5904 4.57237 12.1886C3.88422 12.8767 3.37598 13.7439 3.19652 14.5216C2.65814 16.7947 3.49562 18.4098 4.51255 19.4866ZM4.51255 19.4866L2.00012 21.999" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"></path>
    <path d="M19.4867 4.51472C16.9736 2.12078 13.9929 3.09593 12.7962 4.06548C12.177 4.56712 11.8987 4.89138 11.6593 5.13078C10.8216 5.90881 10.8755 6.68683 11.42 7.30926C11.4983 7.39881 11.7292 7.62975 12.055 7.95281M19.4867 4.51472C20.504 5.59199 21.3528 7.22547 20.8142 9.49971C20.6347 10.2777 20.1264 11.1453 19.438 11.8338C18.8397 12.4323 18.361 13.0128 17.7028 13.0128C16.9847 13.0308 16.6121 12.5115 16.064 11.9651M19.4867 4.51472L21.9999 2.0011M16.064 11.9651C15.6547 11.5701 15.07 10.9721 14.3759 10.2777C13.5175 9.39612 12.6355 8.52831 12.055 7.95281M16.064 11.9651L14.5024 13.4896M10.5114 9.50609L12.055 7.95281" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"></path>
</svg>
</Icon>
Integrate Roslyn for enhanced code highlighting and documentation in your content site
</LinkCard>


<LinkCard Title="Using UI Elements" href="xref:docs.getting-started.using-ui-elements" >
<Icon>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="2em" height="2em">
    <path d="M2.5 12C2.5 7.52166 2.5 5.28249 3.89124 3.89124C5.28249 2.5 7.52166 2.5 12 2.5C16.4783 2.5 18.7175 2.5 20.1088 3.89124C21.5 5.28249 21.5 7.52166 21.5 12C21.5 16.4783 21.5 18.7175 20.1088 20.1088C18.7175 21.5 16.4783 21.5 12 21.5C7.52166 21.5 5.28249 21.5 3.89124 20.1088C2.5 18.7175 2.5 16.4783 2.5 12Z" stroke="currentColor" stroke-width="1.5"></path>
    <path d="M2.5 9H21.5" stroke="currentColor" stroke-width="1.5" stroke-linejoin="round"></path>
    <path d="M13 13L17 13" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
    <path d="M13 17H15" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
    <path d="M6.99981 6H7.00879" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
    <path d="M10.9998 6H11.0088" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
    <path d="M9 9V21.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"></path>
</svg>
</Icon>
Learn how to enhance your site with pre-built UI components from MyLittleContentEngine.UI
</LinkCard>


<LinkCard Title="Deploying to GitHub Pages" href="xref:docs.getting-started.deploying-to-github-pages" Color="primary">
<Icon>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="2em" height="2em" >
    <path d="M6.51734 17.1132C6.91177 17.6905 8.10883 18.9228 9.74168 19.2333M9.86428 22C8.83582 21.8306 2 19.6057 2 12.0926C2 5.06329 8.0019 2 12.0008 2C15.9996 2 22 5.06329 22 12.0926C22 19.6057 15.1642 21.8306 14.1357 22C14.1357 22 13.9267 18.5826 14.0487 17.9969C14.1706 17.4113 13.7552 16.4688 13.7552 16.4688C14.7262 16.1055 16.2043 15.5847 16.7001 14.1874C17.0848 13.1032 17.3268 11.5288 16.2508 10.0489C16.2508 10.0489 16.5318 7.65809 15.9996 7.56548C15.4675 7.47287 13.8998 8.51192 13.8998 8.51192C13.4432 8.38248 12.4243 8.13476 12.0018 8.17939C11.5792 8.13476 10.5568 8.38248 10.1002 8.51192C10.1002 8.51192 8.53249 7.47287 8.00036 7.56548C7.46823 7.65809 7.74917 10.0489 7.74917 10.0489C6.67316 11.5288 6.91516 13.1032 7.2999 14.1874C7.79575 15.5847 9.27384 16.1055 10.2448 16.4688C10.2448 16.4688 9.82944 17.4113 9.95135 17.9969C10.0733 18.5826 9.86428 22 9.86428 22Z" stroke="currentColor" stroke-width="1.25" stroke-linecap="round" stroke-linejoin="round"></path>
</svg>
</Icon>
Deploy your documentation site to GitHub Pages with automated workflows
</LinkCard>
</CardGrid>

## Frequently Asked Questions

Are these docs finished? 
:   No.

Is this app bug-free and ready for production?
:   Not even close.

Is there a migration tool from my favorite documentation tool?
:   No.

Is this an appropriate tool for my non-.NET project?
:   Probably not, but knock yourself out.

Can I submit a pull request?
:   Yeah, but hop on the [GitHub discussions](https://github.com/phil-scott-78/MyLittleContentEngine/discussions) first before getting too far ahead of yourself.

There's a missing feature my company critically needs — it's costing us $1000 a day! 
:   I'll add it for $999 a day.