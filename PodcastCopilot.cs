using System.Web;
using Azure.AI.OpenAI;
using Azure;
using Newtonsoft.Json.Linq;

namespace PodcastAppAPI
{
    public class PodcastCopilot
    {
        //Initializing the Endpoints and Keys
        static string endpointWE = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT_WE");
        static string keyWE = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY_WE");

        static string endpointSC = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT_SC");
        static string keySC = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY_SC");

        static string bingSearchUrl = "https://api.bing.microsoft.com/v7.0/search";
        static string bingSearchKey = Environment.GetEnvironmentVariable("BING_SEARCH_KEY");

        //Instantiate OpenAI Client for Whisper and GPT-3
        static OpenAIClient clientWE = new OpenAIClient(
            new Uri(endpointWE),
            new AzureKeyCredential(keyWE));

        //Instantiate OpenAI Client for Dall.E 3
        static OpenAIClient clientSC = new OpenAIClient(
            new Uri(endpointSC),
            new AzureKeyCredential(keySC));

        //Get Audio Transcription
        public static async Task<string> GetTranscription(string podcastUrl)
        {
            var decodededUrl = HttpUtility.UrlDecode(podcastUrl);

            HttpClient httpClient = new HttpClient();
            Stream audioStreamFromBlob = await httpClient.GetStreamAsync(decodededUrl);

            var transcriptionOptions = new AudioTranscriptionOptions()
            {
                DeploymentName = "whisper",
                AudioData = BinaryData.FromStream(audioStreamFromBlob),
                ResponseFormat = AudioTranscriptionFormat.Verbose,
                Filename = "file.mp3"
            };

            Response<AudioTranscription> transcriptionResponse = await clientWE.GetAudioTranscriptionAsync(
                transcriptionOptions);
            AudioTranscription transcription = transcriptionResponse.Value;

            return transcription.Text;
        }

        //Extract Guest Name from transcription
        public static async Task<string> GetGuestName(string transcription)
        {
            var completionOptions = new ChatCompletionsOptions()
            {
                DeploymentName = "gpt35turbo",
                Messages =
                {
                    new ChatRequestSystemMessage(@"Extract the guest name on the Beyond the Tech podcast from the following transcript.
                        Beyond the Tech is hosted by Kevin Scott and Christina Warren, so they will never be the guests."),
                    new ChatRequestUserMessage(transcription)
                },
                Temperature = (float)0.7
            };

            Response<ChatCompletions> completionsResponse = await clientWE.GetChatCompletionsAsync(
                completionOptions);
            ChatCompletions completion = completionsResponse.Value;

            return completion.Choices[0].Message.Content;
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
            var completionOptions = new ChatCompletionsOptions()
            {
                DeploymentName = "gpt35turbo",
                Messages =
                {
                    new ChatRequestSystemMessage(
                        @"You are a helpful large language model that can create a 
                        LinkedIn promo blurb for episodes of the podcast 
                        Behind the Tech, when given transcripts of the podcasts.
                        The Behind the Tech podcast is hosted by Kevin Scott.\n"
                    ),
                    new ChatRequestUserMessage(
                        @"Create a short summary of this podcast episode 
                        that would be appropriate to post on LinkedIn to    
                        promote the podcast episode. The post should be 
                        from the first-person perspective of Kevin Scott, 
                        who hosts the podcast.\n" +
                        $"Here is the transcript of the podcast episode: {transcription} \n" +
                        $"Here is the bio of the guest: {bio} \n"
                    )
                },
                Temperature = (float)0.7
            };

            Response<ChatCompletions> completionsResponse = await clientWE.GetChatCompletionsAsync(
                completionOptions);
            ChatCompletions completion = completionsResponse.Value;

            return completion.Choices[0].Message.Content;
        }

        //Generate a Dall-E prompt
        public static async Task<string> GetDallEPrompt(string socialBlurb)
        {
            var completionOptions = new ChatCompletionsOptions()
            {
                DeploymentName = "gpt35turbo",
                Messages =
                {
                    new ChatRequestSystemMessage(
                        @"You are a helpful large language model that generates 
                        DALL-E prompts, that when given to the DALL-E model can 
                        generate beautiful high-quality images to use in social 
                        media posts about a podcast on technology. Good DALL-E 
                        prompts will contain mention of related objects, and 
                        will not contain people or words. Good DALL-E prompts 
                        should include a reference to podcasting along with 
                        items from the domain of the podcast guest.\n"
                    ),
                    new ChatRequestUserMessage(
                        $@"Create a DALL-E prompt to create an image to post along 
                        with this social media text: {socialBlurb}"
                    )
                },
                Temperature = (float)0.7
            };

            Response<ChatCompletions> completionsResponse = await clientWE.GetChatCompletionsAsync(
            completionOptions);

            ChatCompletions completion = completionsResponse.Value;

            return completion.Choices[0].Message.Content;
        }

        //Create social media image with a Dall-E
        public static async Task<string> GetImage(string prompt)
        {
            var generationOptions = new ImageGenerationOptions()
            {
                Prompt = prompt + ", high-quality digital art",
                ImageCount = 1,
                Size = ImageSize.Size1024x1024,
                Style = ImageGenerationStyle.Vivid,
                Quality = ImageGenerationQuality.Hd,
                DeploymentName = "dalle3",
                User = "1",
            };

            Response<ImageGenerations> imageGenerations =
                await clientSC.GetImageGenerationsAsync(generationOptions);

            return imageGenerations.Value.Data[0].Url.ToString();
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
