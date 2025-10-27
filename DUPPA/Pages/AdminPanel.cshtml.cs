using DUPPA.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DUPPA.Pages
{
    public class AdminPanel : PageModel
    {
        [BindProperty]
        public string SelectedOption { get; set; }

        [BindProperty]
        public int SelectedImageId { get; set; }

        [BindProperty]
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        
        [BindProperty]
        public string OptionalNote { get; set; }

        public void SaveToDb()
        {
            DataBase.Instance.AddScoreForDay(SelectedOption, SelectedImageId, SelectedDate,OptionalNote);
        }
        
        public void OnGet(string? selectedDate)
        {
            // Try to parse the date from the URL (e.g., /adminPanel?selectedDate=2024-10-27)
            if (DateTime.TryParse(selectedDate, out var date))
            {
                SelectedDate = date;
                if (date > DateTime.Today)
                {
                    SelectedDate = DateTime.Today;
                }
            }
            else
            {
                SelectedDate = DateTime.Today;
            }
            string dayName = SelectedDate.DayOfWeek.ToString();
            SelectedOption = DataBase.Instance.DefaultUserForDay[dayName];
        }

        public IActionResult OnPost()
        {
            if (!string.IsNullOrEmpty(SelectedOption) && SelectedImageId != 0)
            {
                SaveToDb();
                return RedirectToPage("/Index");
            }

            return Page();
        }
    }
}