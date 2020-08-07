﻿using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Amazon.DynamoDb
{
    public sealed class DynamoDbManagementClient : AwsClient
    {
        private const string TargetPrefix = "DynamoDB_20120810";

        private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions {
            Converters = {
                new JsonConverters.DateTimeOffsetConverter()
            },
            IgnoreNullValues = true
        };

        public DynamoDbManagementClient(AwsRegion region, IAwsCredential credential)
            : base(AwsService.DynamoDb, region, credential)
        {
            httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

  
        public async Task<CreateTableResult> CreateTableAsync(CreateTableRequest request)
        {
            return await HandleRequestAsync<CreateTableRequest, CreateTableResult>("CreateTable", request).ConfigureAwait(false);
        }

     
        public async Task<DeleteTableResult> DeleteTableAsync(string tableName)
        {
            return await HandleRequestAsync<TableRequest, DeleteTableResult>("DeleteTable", new TableRequest(tableName)).ConfigureAwait(false);
        }

        public async Task<DescribeTableResult> DescribeTableAsync(string tableName)
        {
            return await HandleRequestAsync<TableRequest, DescribeTableResult>("DescribeTable", new TableRequest(tableName)).ConfigureAwait(false);
        }

        public async Task<DescribeTimeToLiveResult> DescribeTimeToLiveAsync(string tableName)
        {
            return await HandleRequestAsync<TableRequest, DescribeTimeToLiveResult>("DescribeTimeToLive", new TableRequest(tableName)).ConfigureAwait(false);
        }


        public async Task<ListTablesResult> ListTablesAsync(ListTablesRequest request)
        {
            return await HandleRequestAsync<ListTablesRequest, ListTablesResult>("ListTables", request).ConfigureAwait(false);
        }

        public async Task<UpdateTableResult> UpdateTableAsync(UpdateTableRequest request)
        {
            return await HandleRequestAsync<UpdateTableRequest, UpdateTableResult>("UpdateTable", request).ConfigureAwait(false);
        }

        public async Task<UpdateTimeToLiveResult> UpdateTimeToLiveAsync(UpdateTimeToLiveRequest request)
        {
            return await HandleRequestAsync<UpdateTimeToLiveRequest, UpdateTimeToLiveResult>("UpdateTimeToLive", request).ConfigureAwait(false);
        }

        #region Helpers

        private async Task<TResult> HandleRequestAsync<TRequest, TResult>(string action, TRequest request)
        {
            var httpRequest = Setup(action, JsonSerializer.SerializeToUtf8Bytes(request));

            return await SendAndReadObjectAsync<TResult>(httpRequest).ConfigureAwait(false);
        }

        private async Task<T> SendAndReadObjectAsync<T>(HttpRequestMessage request)
        {
            await SignAsync(request).ConfigureAwait(false);

            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw await GetExceptionAsync(response).ConfigureAwait(false);
            }

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            return await JsonSerializer.DeserializeAsync<T>(stream, serializerOptions).ConfigureAwait(false);
        }

        private HttpRequestMessage Setup(string action, byte[]? utf8Json)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) {
                Headers = {
                    { "Accept-Encoding", "gzip" },
                    { "x-amz-target", TargetPrefix + "." + action }
                }
            };

            if (utf8Json != null)
            {
                request.Content = new ByteArrayContent(utf8Json) {
                    Headers = { { "Content-Type", "application/x-amz-json-1.0" } }
                };
            }

            return request;
        }

        protected override async Task<Exception> GetExceptionAsync(HttpResponseMessage response)
        {
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var ex = await DynamoDbException.DeserializeAsync(stream).ConfigureAwait(false);

            ex.StatusCode = (int)response.StatusCode;

            return ex;
        }

        #endregion
    }
}