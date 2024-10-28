﻿using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using ImageCaptionService.Contracts;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace ImageCaptionService.ImageCaptionServices.InferCaption
{
    internal class InferCaptionRestKey : IInferCaption
    {
        private readonly ILogger<InferCaptionRestKey> _logger;
        //private readonly ITokenManager _tokenManager;
        private readonly IConfiguration _configuration;
        private readonly string _endpoint;
        private readonly string _key;
        private readonly string _deploymentOrModelName4;
        private readonly string _visionDeploymentModelName;
        private readonly string _deploymentOrModelNameSecondary;
        private readonly float _temperature;
        private readonly float _nucleus;
        private int _maxTokens = 4096; // Max tokens represents token limit for entire completion: Input and output

        private int _maxOutputTokens = 2048;

        public InferCaptionRestKey(IConfiguration configuration,
                                   ILogger<InferCaptionRestKey> logger)
        {
            _logger = logger;

            _configuration = configuration;

            // Get the Azure OpenAI Service configuration values
            _endpoint = _configuration["ai-endpoint"]
                ?? throw new ArgumentException("ai-endpoint is Missing");

            _visionDeploymentModelName = _configuration["ai-visionmodelname"]
                ?? throw new ArgumentException("ai-visionmodelname is Missing");

            _deploymentOrModelName4 = _configuration["ai-primarymodelname"]
                ?? throw new ArgumentException("ai-primarymodelname is Missing");

            _deploymentOrModelNameSecondary = _configuration["ai-secondarymodelname"]
                ?? throw new ArgumentException("ai-secondarymodelname is Missing");

            _temperature = float.Parse(_configuration["ai-temperature"]
                ?? throw new ArgumentException("ai-temperature"));

            _nucleus = float.Parse(_configuration["ai-nucleus"]
                ?? throw new ArgumentException("ai-nucleus"));

            _key = _configuration["ai-aikey"]
             ?? throw new ArgumentException("ai-aikey");

            _maxTokens = Convert.ToInt32(_configuration["ai-maxtokens"]
                ?? throw new ArgumentException("ai-maxtokens is Missing"));

            _maxOutputTokens = Convert.ToInt32(_configuration["ai-maxoutputtokens"]
                ?? throw new ArgumentException("ai-maxoutputtokens is Missing"));
        }

        public async Task<string> InferImageCaptionAsync(byte[] imageBytes)
        {
            try
            {
                var encodedImage = Convert.ToBase64String(imageBytes);
                var serviceCall = $"{_endpoint}openai/deployments/{_visionDeploymentModelName}/chat/completions?api-version=2024-08-01-preview";

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("api-key", _key);
                    var payload = new
                    {
                        messages = new object[]
                        {
                  new {
                      role = "system",
                      content = new object[] {
                          new {
                              type = "text",
                              text = PromptTemplates.SystemPromptTemplate
                              //text = "You are an assistant that identifies objects in images."
                          }
                      }
                  },
                  new {
                      role = "user",
                      content = new object[] {
                          new {
                              type = "image_url",
                              image_url = new {
                                  url = $"data:image/jpeg;base64,{encodedImage}"
                              }
                          },
                          new {
                              type = "text",
                              text = PromptTemplates.MainPromptTemplate
                              //text = "Please identify the main object in the provided image."
                          }
                      }
                  }
                        },
                        temperature = 0.7,
                        top_p = 0.95,
                        //max_tokens = _maxTokens,
                        stream = false
                    };

                    //var response = await httpClient.PostAsync(ENDPOINT, new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
                    var response = await httpClient.PostAsync(serviceCall, new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        //var responseData = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                        var responseData = JsonConvert.DeserializeObject<OpenAIResponse>(await response.Content.ReadAsStringAsync());

                        if (responseData.Choices.Count == 0)
                        {
                            return string.Empty;
                        }

                        var contentAnswer = responseData.Choices[0].Message.Content;
                        return contentAnswer;
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}, {response.ReasonPhrase}");
                        return string.Empty;
                    }
                }















                //// Set up HttpClient
                //var httpClient = new HttpClient();
                ////httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _key);
                //httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_key}");

                //// Create request with multipart form-data
                //var formData = new MultipartFormDataContent();
                //formData.Add(new ByteArrayContent(imageBytes), "file", "image.png");
                //formData.Add(new StringContent("You are an assistant that identifies objects in images."), "system");
                //formData.Add(new StringContent("Please identify the main object in the provided image."), "user");


                ////https://ai-openai-playground-west.openai.azure.com/openai/deployments/gpt-4-vision/chat/completions?api-version=2024-08-01-preview



                //var serviceCall = $"{_endpoint}openai/deployments/{_visionDeploymentModelName}/chat/completions?api-version=2024-08-01-preview";
                //var response = await httpClient.PostAsync(serviceCall, formData);


                //// Log the status code
                //_logger.LogInformation($"Response Status Code: {response.StatusCode}");

                //// Log the headers
                //foreach (var header in response.Headers)
                //{
                //    _logger.LogInformation($"{header.Key}: {string.Join(", ", header.Value)}");
                //}


                //var responseContent = await response.Content.ReadAsStringAsync();

                //return responseContent;

                //// Send the POST request
                ////var response = await httpClient.PostAsync("https://<your-endpoint>.openai.azure.com/openai/deployments/<your-deployment>/chat/completions?api-version=2023-09-01-preview", formData);
                ////var response = await httpClient.PostAsync($"{_endpoint}openai/deployments/{_visionDeploymentModelName}/chat/completions?api-version=2023-09-01-preview", formData);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Exception throw in InferCaption.InferImageCaptionAsync: {ex.Message}";
                _logger.LogError(errorMessage);
                throw;
            }






            //// Instatiate OpenAIClient
            //AzureOpenAIClient client = new(
            //    new Uri(_endpoint),
            //    new AzureKeyCredential(_key));

            //ChatClient chatClient = client.GetChatClient(_deploymentOrModelName4);

            ////imageBase64 = imageBase64.Replace("data:image/jpeg;base64,", base64);
            //imageBase64 = string.Concat("data:image/jpeg;base64,", base64);

            //var messages = new List<ChatMessage>();
            //messages.Add(new SystemChatMessage(PromptTemplates.SystemPromptTemplate));
            //messages.Add(new UserChatMessage(PromptTemplates.MainPromptTemplate.Replace("{{$base64_string}}", imageBase64)));

            //ChatCompletionOptions completionOptions = new ChatCompletionOptions
            //{
            //    Temperature = _temperature,
            //    //MaxTokens = _maxTokens,
            //    TopP = _nucleus,
            //};

            //ChatCompletion chatCompletion = chatClient.CompleteChat(messages, completionOptions);

            //var ContentFilterReason = chatCompletion.FinishReason;

            //return chatCompletion.Content[0].Text;

        }


        public class OpenAIResponse
        {
            public List<Choice> Choices { get; set; }
        }

        public class Choice
        {
            public Message Message { get; set; }
        }

        public class Message
        {
            public string Content { get; set; }
        }
    }
}