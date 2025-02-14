﻿#nullable disable

using System.Xml.Serialization;

namespace Amazon.Sqs;

public sealed class SendMessageBatchResponse
{
    [XmlElement("SendMessageBatchResult")]
    public SendMessageBatchResult SendMessageBatchResult { get; init; }

    public static SendMessageBatchResponse Parse(string xmlText)
    {
        return SqsSerializer<SendMessageBatchResponse>.Deserialize(xmlText);
    }
}

/*
<SendMessageBatchResponse>
	<SendMessageBatchResult>
		<SendMessageBatchResultEntry>
			<Id>test_msg_001</Id>
			<MessageId>0a5231c7-8bff-4955-be2e-8dc7c50a25fa</MessageId>
			<MD5OfMessageBody>0e024d309850c78cba5eabbeff7cae71</MD5OfMessageBody>
		</SendMessageBatchResultEntry>
		<SendMessageBatchResultEntry>
			<Id>test_msg_002</Id>
			<MessageId>15ee1ed3-87e7-40c1-bdaa-2e49968ea7e9</MessageId>
			<MD5OfMessageBody>7fb8146a82f95e0af155278f406862c2</MD5OfMessageBody>
		</SendMessageBatchResultEntry>
	</SendMessageBatchResult>

	<ResponseMetadata>
		<RequestId>ca1ad5d0-8271-408b-8d0f-1351bf547e74</RequestId>
	</ResponseMetadata>
</SendMessageBatchResponse>
*/
