

namespace Util.Core
{
	public class DefaultResponse
	{
		public bool Success { get; protected set; }
		public int StatusCode { get; set; }
		public dynamic Data { get; set; }
		public string Message { get; set; }

		public DefaultResponse(bool success, int statusCode, dynamic data, string message)
		{
			Success = success;
			StatusCode = statusCode;
			Data = data;
			Message = message;
		}
	}
}
