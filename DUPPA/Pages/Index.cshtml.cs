using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using DUPPA.Database;
using DUPPA.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DUPPA.Pages
{
    public class IndexModel : PageModel
    {
        public string SelectedMonth { get; set; }
        public string LastMonth { get; set; }
        public List<string> Months { get; set; }
        public List<(string Name, double AverageScore)> UserStats { get; set; }

        public List<(string Name, double AverageScore)> Winners { get; set; }
        public List<(string Name, double AverageScore)> Losers { get; set; }
        
        public Dictionary<DateTime, (string UserName, int Score, string? Note)> CalendarScores { get; set; } = new();

        public void OnGet(string? month)
        {
            Months = DateTimeFormatInfo.CurrentInfo!.MonthNames
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToList();

            SelectedMonth = month ?? DateTime.Now.ToString("MMMM");
            var lastMonthDate = DateTime.Now.AddMonths(-1);
            LastMonth = lastMonthDate.ToString("MMMM");
            var users = DataBase.Instance.GetUsers();
            UserStats = users.Select(user => (
                Name: user.Name,
                AverageScore: user.Scores
                    .Where(score => score.CreatedAt.Month == DateTime.ParseExact(SelectedMonth, "MMMM", CultureInfo.CurrentCulture).Month)
                    .DefaultIfEmpty(new Score(5, DateTime.MinValue))
                    .Average(score => score.Value)
            )).ToList();

            var lastMonthStats = users.Select(user =>
            {
                var scores = user.Scores
                    .Where(score => score.CreatedAt.Month == lastMonthDate.Month)
                    .ToList();

                double average = scores.Count > 0 ? scores.Average(s => s.Value) : -1;
                return (Name: user.Name, AverageScore: average);
            }).ToList();

            var maxScore = lastMonthStats.Max(u => u.AverageScore);
            var minScore = lastMonthStats.Min(u => u.AverageScore);

            Winners = lastMonthStats
                .Where(u => u.AverageScore!= -1 && Math.Abs(u.AverageScore - maxScore) < 0.001)
                .ToList();

            Losers = lastMonthStats
                .Where(u => u.AverageScore!= -1 && Math.Abs(u.AverageScore - minScore) < 0.001)
                .ToList();
            
            
            var selectedMonthDate = DateTime.ParseExact(SelectedMonth, "MMMM", CultureInfo.CurrentCulture);
            var firstDayOfMonth = new DateTime(DateTime.Now.Year, selectedMonthDate.Month, 1);

            CalendarScores = DataBase.Instance.GetAllScores()
                .Where(s => s.CreatedAt.Month == firstDayOfMonth.Month && s.CreatedAt.Year == firstDayOfMonth.Year)
                .ToDictionary(s => s.CreatedAt.Date, s => (s.UserName, s.Value, s.Note));
        }
    }

}