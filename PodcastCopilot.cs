using System.Web;
using Azure.AI.OpenAI;
using Azure;
using Newtonsoft.Json.Linq;
using OpenAI.Chat;
using OpenAI.Audio;
using OpenAI.Images;

namespace PodcastAppAPI
{
    public class PodcastCopilot
    {
        //Initialize Endpoints and Key
        static string endpointSC = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT_SC");
        static string keySC = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY_SC");

        static string bingSearchUrl = "https://api.bing.microsoft.com/v7.0/search";
        static string bingSearchKey = Environment.GetEnvironmentVariable("BING_SEARCH_KEY");

        //Instantiate OpenAI Client
        static AzureOpenAIClient azureOpenAIClient = new AzureOpenAIClient(
            new Uri(endpointSC),
            new AzureKeyCredential(keySC));

        //Get Audio Transcription
        public static async Task<string> GetTranscription(string podcastUrl)
        {
            var decodededUrl = HttpUtility.UrlDecode(podcastUrl);

            HttpClient httpClient = new HttpClient();
            Stream audioStreamFromBlob = await httpClient.GetStreamAsync(decodededUrl);

            AudioClient client = azureOpenAIClient.GetAudioClient("whisper");
            AudioTranscription audioTranscription =
                await client.TranscribeAudioAsync(audioStreamFromBlob, "file.mp3");

            return audioTranscription.Text;
        }

        //Extract Guest Name from transcription
        public static async Task<string> GetGuestName(string transcription)
        {
            ChatClient client = azureOpenAIClient.GetChatClient("gpt4");

            ChatCompletion chatCompletion = await client.CompleteChatAsync(
            [
                    new SystemChatMessage("Extract only the guest name on the Beyond the Tech podcast from the following transcript. Beyond the Tech is hosted by Kevin Scott, so Kevin Scott will never be the guest."),
                    new UserChatMessage(transcription)
            ]);

            return chatCompletion.ToString();
        }

        //Get Guest Bio from Bing
        public static async Task<string> GetGuestBio(string guestName)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", bingSearchKey);

            HttpResponseMessage response = await client.GetAsync($"{bingSearchUrl}?q={guestName}");

            string responseBody = await response.Content.ReadAsStringAsync();

            // Parse responseBody as JSON and extract the bio.
            JObject searchResults = JObject.Parse(responseBody);
            var bio = searchResults["webPages"]["value"][0]["snippet"].ToString();

            return bio;
        }

        //Create Social Media Blurb
        public static async Task<string> GetSocialMediaBlurb(string transcription, string bio)
        {
            ChatClient client = azureOpenAIClient.GetChatClient("gpt4");

            ChatCompletion chatCompletion = await client.CompleteChatAsync(
            [
                new SystemChatMessage(
                    @"You are a helpful large language model that can create a LinkedIn 
                    promo blurb for episodes of the podcast Behind the Tech, when given 
                    transcripts of the podcasts. The Behind the Tech podcast is hosted 
                    by Kevin Scott."),
                new UserChatMessage(
                    @"Create a short summary of this podcast episode that would be appropriate 
                    to post on LinkedIn to promote the podcast episode. The post should be from 
                    the first-person perspective of Kevin Scott, who hosts the podcast. \n" +
                    $"Here is the transcript of the podcast episode: {transcription} \n" +
                    $"Here is the bio of the guest: {bio}")
            ]);

            return chatCompletion.ToString();
        }

        //Generate a Dall-E prompt
        public static async Task<string> GetDallEPrompt(string socialBlurb)
        {
            ChatClient client = azureOpenAIClient.GetChatClient("gpt4");

            ChatCompletion chatCompletion = await client.CompleteChatAsync(
            [
                new SystemChatMessage(
                    @"You are a helpful large language model that generates DALL-E prompts, 
                    that when given to the DALL-E model can generate beautiful high-quality 
                    images to use in social media posts about a podcast on technology. Good 
                    DALL-E prompts will contain mention of related objects, and will not contain 
                    people, faces, or words. Good DALL-E prompts should include a reference 
                    to podcasting along with items from the domain of the podcast guest."),
                new UserChatMessage(
                    @$"Create a DALL-E prompt to create an image to post along with this social 
                    media text: {socialBlurb}")

            ]);

            return chatCompletion.ToString();
        }

        //Create social media image with a Dall-E
        public static async Task<string> GetImage(string prompt)
        {
            ImageClient client = azureOpenAIClient.GetImageClient("dalle3");

            ImageGenerationOptions options = new()
            {
                Quality = GeneratedImageQuality.High,
                Size = GeneratedImageSize.W1024xH1024,
                Style = GeneratedImageStyle.Vivid,
                ResponseFormat = GeneratedImageFormat.Uri,
            };

            GeneratedImage image =
                await client.GenerateImageAsync(prompt + ", high-quality digital art", options);

            return image.ImageUri.ToString();
        }

        public static async Task<SocialMediaPost> GenerateSocialMediaPost(string podcastUrl)
        {
            var transcription = await GetTranscription(podcastUrl);
            var guestName = await GetGuestName(transcription);
            var guestBio = await GetGuestBio(guestName);
            var generatedBlurb = await GetSocialMediaBlurb(transcription, guestBio);
            var dallePrompt = await GetDallEPrompt(generatedBlurb);
            var generatedImage = await GetImage(dallePrompt);

            var socialMediaPost = new SocialMediaPost()
            {
                ImageUrl = generatedImage,
                Blurb = generatedBlurb
            };

            return socialMediaPost;
        }
    }
}
