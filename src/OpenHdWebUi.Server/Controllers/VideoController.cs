using System.Text;
using Bld.RtpToWebRtcRestreamer.Restreamer;
using Microsoft.AspNetCore.Mvc;
using OpenHdWebUi.Server.Services.AirGround;

namespace OpenHdWebUi.Server.Controllers
{
    [Route("api/video")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        private readonly IRtpRestreamerControl _control;
        private readonly AirGroundService _airGroundService;

        public VideoController(
            IRtpRestreamerControl control,
            AirGroundService airGroundService)
        {
            _control = control;
            _airGroundService = airGroundService;
        }

        [HttpPost("stop")]
        public async Task Stop()
        {
            await _control.StopAsync();
        }

        [HttpPost("sdp")]
        [Produces("application/sdp")]
        [Consumes("application/sdp")]
        public async Task Post()
        {
            if (!_airGroundService.IsGroundMode)
            {
                return;
            }

            using var streamReader = new StreamReader(Request.Body, Encoding.UTF8);
            var sdpString = await streamReader.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(sdpString))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var answer = await _control.AppendClient();

            Response.StatusCode = StatusCodes.Status201Created;
            Response.Headers.Location = $"api/video/sdp/{answer.PeerId}";
            await Response.WriteAsync(answer.Sdp, Encoding.UTF8);
        }

        [HttpPatch("sdp/{peerId}")]
        [Consumes("application/sdp")]
        public async Task Patch([FromRoute] Guid peerId)
        {
            if (!_airGroundService.IsGroundMode)
            {
                return;
            }

            using var streamReader = new StreamReader(Request.Body, Encoding.UTF8);
            var sdpString = await streamReader.ReadToEndAsync();

            await _control.ProcessClientAnswerAsync(peerId, sdpString);

            Response.StatusCode = StatusCodes.Status200OK;
        }
    }
}