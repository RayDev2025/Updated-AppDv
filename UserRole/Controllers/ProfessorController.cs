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

            // Get all section assignments for this professor
            var allAssignments = new List<ProfessorAssignmentInfo>();

            // Add primary assignment if exists
            if (!string.IsNullOrEmpty(user.AssignedGradeLevel))
            {
                var primaryAssignment = new ProfessorAssignmentInfo
                {
                    GradeLevel = user.AssignedGradeLevel,
                    Section = user.AssignedSection ?? 0,
                    Subject = user.AssignedSubject ?? "All Subjects",
                    Room = user.AssignedRoom ?? "No room assigned",
                    IsPrimary = true
                };
                allAssignments.Add(primaryAssignment);
            }

            // Get additional assignments from ProfessorSectionAssignments table
            var additionalAssignments = await _context.ProfessorSectionAssignments
                .Where(a => a.ProfessorId == user.Id)
                .Select(a => new ProfessorAssignmentInfo
                {
                    GradeLevel = a.GradeLevel,
                    Section = a.Section,
                    Subject = a.Subject ?? "All Subjects",
                    Room = a.AssignedRoom ?? "No room assigned",
                    IsPrimary = false
                })
                .ToListAsync();

            allAssignments.AddRange(additionalAssignments);

            // Get all students from all assigned sections
            var allStudents = new List<Enrollment>();
            foreach (var assignment in allAssignments)
            {
                var students = await _context.Enrollments
                    .Include(e => e.User)
                    .Where(e => e.GradeLevel == assignment.GradeLevel &&
                               e.Section == assignment.Section &&
                               e.Status == "approved")
                    .ToListAsync();

                // Avoid duplicate students
                foreach (var student in students)
                {
                    if (!allStudents.Any(s => s.Id == student.Id))
                    {
                        allStudents.Add(student);
                    }
                }
            }

            // Get schedule from primary assignment
            var schedule = allAssignments.FirstOrDefault()?.GradeLevel != null
                ? GetClassSchedule(allAssignments.First().GradeLevel)
                : new ClassScheduleViewModel();

            var viewModel = new ProfessorDashboardViewModel
            {
                ProfessorName = user.FullName,
                GradeLevel = user.AssignedGradeLevel ?? "Not Assigned",
                Section = user.AssignedSection ?? 0,
                AssignedRoom = user.AssignedRoom ?? "No room assigned",
                Students = allStudents.OrderBy(s => s.GradeLevel).ThenBy(s => s.Section).ThenBy(s => s.StudentName).ToList(),
                ClassSchedule = schedule,
                TotalStudents = allStudents.Count,
                Assignments = allAssignments.OrderBy(a => a.GradeLevel).ThenBy(a => a.Section).ToList()
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

            // Check if student is in any of professor's assigned sections
            var student = await _context.Enrollments
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (student == null)
            {
                TempData["Error"] = "Student not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Verify professor has access to this student
            var hasAccess = false;

            // Check primary assignment
            if (student.GradeLevel == user.AssignedGradeLevel &&
                student.Section == user.AssignedSection)
            {
                hasAccess = true;
            }

            // Check additional assignments
            if (!hasAccess)
            {
                hasAccess = await _context.ProfessorSectionAssignments
                    .AnyAsync(a => a.ProfessorId == user.Id &&
                                  a.GradeLevel == student.GradeLevel &&
                                  a.Section == student.Section);
            }

            if (!hasAccess)
            {
                TempData["Error"] = "You don't have access to view this student.";
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

    // Helper class for assignment info
    public class ProfessorAssignmentInfo
    {
        public string GradeLevel { get; set; }
        public int Section { get; set; }
        public string Subject { get; set; }
        public string Room { get; set; }
        public bool IsPrimary { get; set; }
    }
}