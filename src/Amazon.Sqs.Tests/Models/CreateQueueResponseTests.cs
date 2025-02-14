﻿namespace Amazon.Sqs.Models.Tests
{
    public class CreateQueueResponseTests
    {
        [Fact]
        public void Parse()
        {
            var xmlText =

            @"<CreateQueueResponse xmlns=""http://queue.amazonaws.com/doc/2012-11-05/"">
				<CreateQueueResult>
					<QueueUrl>http://queue.amazonaws.com/1234/hello</QueueUrl>
				</CreateQueueResult>
			</CreateQueueResponse>";

            var result = CreateQueueResponse.Parse(xmlText).CreateQueueResult;

            Assert.Equal("http://queue.amazonaws.com/1234/hello", result.QueueUrl);
        }
    }
}