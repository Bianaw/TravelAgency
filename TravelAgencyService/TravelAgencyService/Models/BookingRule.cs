using System.ComponentModel.DataAnnotations;

namespace TravelAgencyService.Models
{
    public class BookingRule
    {
        public int Id { get; set; }

        // כמה ימים לפני התחלת הטיול אפשר להזמין (למשל 1 = עד יום לפני)
        [Range(0, 365)]
        public int LatestBookingDaysBeforeStart { get; set; } = 1;

        // כמה ימים לפני התחלת הטיול אפשר לבטל
        [Range(0, 365)]
        public int CancellationDaysBeforeStart { get; set; } = 5;

        // כמה ימים לפני תחילת הטיול לשלוח תזכורת (נשאיר לוגיקה לאחר כך)
        [Range(0, 365)]
        public int ReminderDaysBeforeStart { get; set; } = 5;

        // מגבלת הזמנות פעילות (לפי מסמך: עד 3)
        [Range(1, 10)]
        public int MaxActiveBookings { get; set; } = 3;
    }
}
