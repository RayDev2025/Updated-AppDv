using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserRoles.Data;
using UserRoles.Models;
using UserRoles.Helpers;

namespace UserRoles.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            AppDbContext context,
            UserManager<Users> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Admin()
        {
            return View();
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> User()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }


            var enrollment = await _context.Enrollments
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.UserId == userId);


            Users assignedProfessor = null; // For grades 1-3
            List<Users> assignedProfessors = new List<Users>(); // For grades 4-6
            string assignedRoom = null;
            string classSchedule = null;
            string shift = null;

            if (enrollment != null && enrollment.Status == "approved")
            {
                var grade = int.Parse(enrollment.GradeLevel);
                bool isMorningShift = grade % 2 == 0;
                shift = isMorningShift ? "Morning" : "Afternoon";
                classSchedule = isMorningShift ? "7:00 AM - 12:00 PM" : "1:00 PM - 6:00 PM";

                // Grades 1-3: One professor per grade (no section)
                if (grade >= 1 && grade <= 3)
                {
                    assignedProfessor = await _context.Users
                        .FirstOrDefaultAsync(u => u.Role == "professor" &&
                                                 u.AssignedGradeLevel == enrollment.GradeLevel);

                    assignedRoom = RoomAssignmentHelper.GetRoomForSection(enrollment.GradeLevel, enrollment.Section ?? 1);
                }
                // Grades 4-6: Multiple professors per section (one per subject)
                else if (grade >= 4 && grade <= 6 && enrollment.Section.HasValue)
                {
                    assignedProfessors = await _context.Users
                        .Where(u => u.Role == "professor" &&
                                   u.AssignedGradeLevel == enrollment.GradeLevel &&
                                   u.AssignedSection == enrollment.Section)
                        .OrderBy(u => u.AssignedSubject)
                        .ToListAsync();

                    if (assignedProfessors.Any() && !string.IsNullOrEmpty(assignedProfessors.First().AssignedRoom))
                    {
                        assignedRoom = assignedProfessors.First().AssignedRoom;
                    }
                    else
                    {
                        assignedRoom = RoomAssignmentHelper.GetRoomForSection(enrollment.GradeLevel, enrollment.Section.Value);
                    }
                }
            }

            ViewBag.Enrollment = enrollment;
            ViewBag.AssignedProfessor = assignedProfessor;
            ViewBag.AssignedProfessors = assignedProfessors;
            ViewBag.AssignedRoom = assignedRoom;
            ViewBag.ClassSchedule = classSchedule;
            ViewBag.Shift = shift;

            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}