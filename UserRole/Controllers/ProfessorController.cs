using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserRoles.Data;
using UserRoles.Models;
using UserRoles.ViewModels;

namespace UserRoles.Controllers
{
    [Authorize]
    public class ProfessorController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly ILogger<ProfessorController> _logger;

        public ProfessorController(
            AppDbContext context,
            UserManager<Users> userManager,
            ILogger<ProfessorController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Professor Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "professor")
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var gradeLevel = user.AssignedGradeLevel;
            var section = user.AssignedSection;

            if (string.IsNullOrEmpty(gradeLevel))
            {
                ViewBag.Error = "No grade level assigned. Please contact administrator.";
                return View(new ProfessorDashboardViewModel());
            }

            var students = await _context.Enrollments
                .Include(e => e.User)
                .Where(e => e.GradeLevel == gradeLevel &&
                           e.Section == section &&
                           e.Status == "approved")
                .OrderBy(e => e.StudentName)
                .ToListAsync();

            var schedule = GetClassSchedule(gradeLevel);

            var viewModel = new ProfessorDashboardViewModel
            {
                ProfessorName = user.FullName,
                GradeLevel = gradeLevel,
                Section = section ?? 0,
                AssignedRoom = user.AssignedRoom ?? "No room assigned", // NEW
                Students = students,
                ClassSchedule = schedule,
                TotalStudents = students.Count
            };

            return View(viewModel);
        }

        // View Student Details
        public async Task<IActionResult> ViewStudent(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "professor")
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var student = await _context.Enrollments
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id &&
                                         e.GradeLevel == user.AssignedGradeLevel &&
                                         e.Section == user.AssignedSection);

            if (student == null)
            {
                TempData["Error"] = "Student not found or not in your section.";
                return RedirectToAction(nameof(Dashboard));
            }

            return View(student);
        }

        // Helper method to get class schedule
        private ClassScheduleViewModel GetClassSchedule(string gradeLevel)
        {
            var grade = int.Parse(gradeLevel);

            // Morning shift: Grades 2, 4, 6
            // Afternoon shift: Grades 1, 3, 5
            bool isMorningShift = grade % 2 == 0;

            return new ClassScheduleViewModel
            {
                GradeLevel = gradeLevel,
                Schedule = isMorningShift ? "7:00 AM - 12:00 PM" : "1:00 PM - 6:00 PM",
                StartTime = isMorningShift ? "7:00 AM" : "1:00 PM",
                EndTime = isMorningShift ? "12:00 PM" : "6:00 PM",
                Shift = isMorningShift ? "Morning" : "Afternoon"
            };
        }
    }
}