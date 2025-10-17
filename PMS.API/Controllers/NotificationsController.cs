using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.DTOs.Notification;
using PMS.Application.Services.Notification;

namespace PMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// https://localhost:7213/api/Notifications/send
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("send")]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không thể xác thực người dùng." });

            await _notificationService.SendNotificationToRolesAsync(
                   userId,
                   request.TargetRoles,
                   request.Title,
                   request.Message,
                   request.Type);

            return Ok("Notification sent to roles");
        }

        /// <summary>
        /// https://localhost:7213/api/Notifications/GetUserNotifi
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetUserNotifi")]
        public async Task<IActionResult> GetUserNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không thể xác thực người dùng." });

            var notifications = await _notificationService.GetNotificationsForUserAsync(userId);
            return Ok(notifications);
        }

        /// <summary>
        /// https://localhost:7213/api/Notifications/read/{notificationId}
        /// </summary>
        /// <param name="notificationId"></param>
        /// <returns></returns>
        [HttpPost("read/{notificationId}")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            await _notificationService.MarkNotificationAsReadAsync(notificationId);
            return Ok();
        }
    }
}
