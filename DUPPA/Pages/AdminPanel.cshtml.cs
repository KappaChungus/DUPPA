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
        public DateTime SelectedDate { get; set; } = DateTime.Today.AddDays(-1);
        
        [BindProperty]
        public string OptionalNote { get; set; }

        public void SaveToDb()
        {
            DataBase.Instance.AddScoreForDay(SelectedOption, SelectedImageId, SelectedDate,OptionalNote);
        }
        
        public void OnGet()
        {
            HttpContext.Session.SetString("IsAdmin", "true");
            SelectedOption = DataBase.Instance._defaultUserForDay[SelectedDate.DayOfWeek.ToString()];
        }

        public IActionResult OnPost()
        {
            HttpContext.Session.SetString("IsAdmin", "true");
            if (!string.IsNullOrEmpty(SelectedOption) && SelectedImageId != 0)
            {
                SaveToDb();
                return RedirectToPage("/Index");
            }

            return Page();
        }
    }
}