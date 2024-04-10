# PodcastAppAPI Sample Project

This is a sample .NET project to be used to test the Power Platform. This API allows you to enter a link to your podcast and then it generates a promotional social media image and blub using the Azure OpenAI Service.

### How it works

1. Starting from the podcast URL, you get the transcription with the Whisper model
2. Given that transcript, you use GPT to extract the name of the guest
3. With the guest name, you retrieve their bio with Bing Search
4. With the transcription and the guest bio, you generate a social media blurb with GPT
5. With the social media blurb, you generate a relevant DALL-E prompt with GPT
6. Finally, you use DALL-E to generate an image for the social media post with the prompt

## Full Workshop

If you want to see the full workshop; you can find it here: [Podcast Copilot with Azure OpenAI Service, .NET, and Power Platform Workshop](https://aka.ms/PowerPodcastCopilot)

This workshop explores the integration of the Power Platform with advanced AI models to create a dynamic application inspired by Kevin Scott’s Microsoft Build 2023 demo (https://github.com/microsoft/podcastcopilot).

In this workshop, you will:
- Setup your Development and Power Platform environments
- Learn about the Azure OpenAI Service and create model deployments
- Build this .NET API using the .NET Azure OpenAI SDK and creating a Custom Connector from Visual Studio
- And finally integrating the API with Power Apps and Copilot Studio
