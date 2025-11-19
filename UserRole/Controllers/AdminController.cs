using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserRole.Services;
using UserRoles.Data;
using UserRoles.Helpers;
using UserRoles.Models;
using UserRoles.ViewModels;

namespace UserRoles.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly IEmailServices _emailService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            AppDbContext context,
            UserManager<Users> userManager,
            IEmailServices emailService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        // Dashboard with statistics
        public async Task<IActionResult> Dashboard()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new AdminDashboardViewModel
            {
                TotalStudents = await _context.Enrollments.CountAsync(e => e.Status == "approved"),
                PendingEnrollments = await _context.Enrollments.CountAsync(e => e.Status == "pending"),
                TotalProfessors = await _context.Users.CountAsync(u => u.Role == "professor"),
                TotalSections = 48, // 8 sections per grade × 6 grades

                // Students per grade level
                StudentsPerGradeLevel = await _context.Enrollments
                    .Where(e => e.Status == "approved")
                    .GroupBy(e => e.GradeLevel)
                    .Select(g => new GradeLevelStats
                    {
                        GradeLevel = g.Key,
                        StudentCount = g.Count()
                    })
                    .OrderBy(g => g.GradeLevel)
                    .ToListAsync(),

                // Professors per grade level
                ProfessorsPerGradeLevel = await _context.Users
                    .Where(u => u.Role == "professor")
                    .GroupBy(u => u.AssignedGradeLevel)
                    .Select(g => new GradeLevelStats
                    {
                        GradeLevel = g.Key,
                        ProfessorCount = g.Count()
                    })
                    .OrderBy(g => g.GradeLevel)
                    .ToListAsync(),

                // Recent enrollments
                RecentEnrollments = await _context.Enrollments
                    .Include(e => e.User)
                    .OrderByDescending(e => e.EnrollmentDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // View all students
        public async Task<IActionResult> ViewStudents(string status = "all")
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return RedirectToAction("Index", "Home");
            }

            IQueryable<Enrollment> query = _context.Enrollments.Include(e => e.User);

            if (status != "all")
            {
                query = query.Where(e => e.Status == status);
            }

            var students = await query.OrderByDescending(e => e.EnrollmentDate).ToListAsync();
            ViewBag.CurrentFilter = status;

            return View(students);
        }

        // Accept enrollment
        // Accept enrollment
        // GET: Show enrollment form with subject selection
        [HttpGet]
        public async Task<IActionResult> EnrollStudent(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment == null)
            {
                return NotFound();
            }

            if (enrollment.Status != "pending")
            {
                TempData["Error"] = "This enrollment has already been processed.";
                return RedirectToAction(nameof(ViewStudents));
            }

            var gradeLevel = int.Parse(enrollment.GradeLevel);
            var viewModel = new EnrollStudentViewModel
            {
                EnrollmentId = enrollment.Id,
                StudentName = enrollment.StudentName,
                GradeLevel = enrollment.GradeLevel,
                Section = enrollment.Section ?? 0,
                ParentName = enrollment.ParentName,
                ContactNumber = enrollment.ContactNumber,
                Address = enrollment.Address
            };

            // Get available professors based on grade level
            if (gradeLevel >= 1 && gradeLevel <= 3)
            {
                // Grades 1-3: Get the single professor for this grade
                var professor = await _context.Users
                    .FirstOrDefaultAsync(u => u.Role == "professor" &&
                                             u.AssignedGradeLevel == enrollment.GradeLevel);

                viewModel.AvailableProfessors["All Subjects"] = new List<ProfessorOption>();
                if (professor != null)
                {
                    viewModel.AvailableProfessors["All Subjects"].Add(new ProfessorOption
                    {
                        Id = professor.Id,
                        Name = professor.FullName,
                        Subject = "All Subjects"
                    });
                }
            }
            else if (gradeLevel >= 4 && gradeLevel <= 6)
            {
                // Grades 4-6: Get professors for each subject in this section
                var subjects = new[] { "Math", "Science", "English", "Filipino", "Social Studies", "MAPEH" };

                foreach (var subject in subjects)
                {
                    var professors = await _context.Users
                        .Where(u => u.Role == "professor" &&
                                   u.AssignedGradeLevel == enrollment.GradeLevel &&
                                   u.AssignedSection == enrollment.Section &&
                                   u.AssignedSubject == subject)
                        .Select(u => new ProfessorOption
                        {
                            Id = u.Id,
                            Name = u.FullName,
                            Subject = subject
                        })
                        .ToListAsync();

                    // Also check ProfessorSectionAssignments
                    var additionalProfessors = await _context.ProfessorSectionAssignments
                        .Where(a => a.GradeLevel == enrollment.GradeLevel &&
                                   a.Section == enrollment.Section &&
                                   a.Subject == subject)
                        .Select(a => new ProfessorOption
                        {
                            Id = a.ProfessorId,
                            Name = a.Professor.FullName,
                            Subject = subject
                        })
                        .ToListAsync();

                    var allProfessors = professors.Union(additionalProfessors)
                        .GroupBy(p => p.Id)
                        .Select(g => g.First())
                        .ToList();

                    viewModel.AvailableProfessors[subject] = allProfessors;
                    viewModel.SubjectProfessors[subject] = "";
                }
            }

            return View(viewModel);
        }

        // POST: Process enrollment with subject assignments
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollStudent(EnrollStudentViewModel model)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == model.EnrollmentId);

            if (enrollment == null)
            {
                return NotFound();
            }

            var gradeLevel = int.Parse(enrollment.GradeLevel);

            // Validate professor assignments
            if (gradeLevel >= 1 && gradeLevel <= 3)
            {
                if (string.IsNullOrEmpty(model.SingleProfessorId))
                {
                    TempData["Error"] = "Please select a professor for this student.";
                    return RedirectToAction(nameof(EnrollStudent), new { id = model.EnrollmentId });
                }
            }
            else if (gradeLevel >= 4 && gradeLevel <= 6)
            {
                var subjects = new[] { "Math", "Science", "English", "Filipino", "Social Studies", "MAPEH" };
                foreach (var subject in subjects)
                {
                    if (!model.SubjectProfessors.ContainsKey(subject) ||
                        string.IsNullOrEmpty(model.SubjectProfessors[subject]))
                    {
                        TempData["Error"] = $"Please select a professor for {subject}.";
                        return RedirectToAction(nameof(EnrollStudent), new { id = model.EnrollmentId });
                    }
                }
            }

            // Update enrollment status
            enrollment.Status = "approved";
            await _context.SaveChangesAsync();

            // Create subject enrollments
            if (gradeLevel >= 1 && gradeLevel <= 3)
            {
                // Single professor for all subjects
                var subjectEnrollment = new SubjectEnrollment
                {
                    EnrollmentId = enrollment.Id,
                    Subject = "All Subjects",
                    ProfessorId = model.SingleProfessorId,
                    EnrolledDate = DateTime.UtcNow
                };
                _context.SubjectEnrollments.Add(subjectEnrollment);
            }
            else if (gradeLevel >= 4 && gradeLevel <= 6)
            {
                // Multiple professors for different subjects
                foreach (var subjectProf in model.SubjectProfessors)
                {
                    var subjectEnrollment = new SubjectEnrollment
                    {
                        EnrollmentId = enrollment.Id,
                        Subject = subjectProf.Key,
                        ProfessorId = subjectProf.Value,
                        EnrolledDate = DateTime.UtcNow
                    };
                    _context.SubjectEnrollments.Add(subjectEnrollment);
                }
            }

            await _context.SaveChangesAsync();

            // Get assigned professors for email
            var assignedProfessors = await _context.SubjectEnrollments
                .Where(se => se.EnrollmentId == enrollment.Id)
                .Include(se => se.Professor)
                .ToListAsync();

            // Get room assignment
            string assignedRoom = RoomAssignmentHelper.GetRoomForSection(enrollment.GradeLevel, enrollment.Section.Value);

            // Get class schedule
            bool isMorningShift = gradeLevel % 2 == 0;
            string shift = isMorningShift ? "Morning" : "Afternoon";
            string classSchedule = isMorningShift ? "7:00 AM - 12:00 PM" : "1:00 PM - 6:00 PM";

            // Send acceptance email with subject details
            try
            {
                string professorsHtml = "";
                if (gradeLevel >= 1 && gradeLevel <= 3)
                {
                    var prof = assignedProfessors.FirstOrDefault()?.Professor;
                    professorsHtml = $@"
                <div class='detail-item'>
                    <strong>Class Adviser</strong>
                    <span>{prof?.FullName ?? "To Be Assigned"}</span>
                </div>";
                }
                else
                {
                    professorsHtml = @"
                <div class='detail-item' style='grid-column: 1 / -1;'>
                    <strong>Subject Teachers</strong>
                    <div style='margin-top: 10px; display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 10px;'>";

                    foreach (var se in assignedProfessors.OrderBy(s => s.Subject))
                    {
                        professorsHtml += $@"
                        <div style='background: white; padding: 10px; border-radius: 6px; border-left: 3px solid #6366f1;'>
                            <strong style='color: #1f2937; display: block; font-size: 0.9em;'>{se.Subject}</strong>
                            <span style='color: #64748b; font-size: 0.85em;'>{se.Professor?.FullName ?? "TBA"}</span>
                        </div>";
                    }

                    professorsHtml += @"
                    </div>
                </div>";
                }

                string subject = "🎉 Enrollment Approved - Elementary School";
                string htmlMessage = $@"
            <html>
            <head>
                <style>
                    /* [Keep the same CSS styles from the original AcceptEnrollment email] */
                </style>
            </head>
            <body>
                <div class='email-container'>
                    <div class='header'>
                        <div class='emoji'>🎓</div>
                        <h1>Enrollment Approved!</h1>
                        <p style='margin: 10px 0 0; opacity: 0.95;'>Elementary School</p>
                    </div>
                    
                    <div class='content'>
                        <div class='greeting'>
                            Dear <strong>{enrollment.ParentName}</strong>,
                        </div>
                        
                        <div class='success-message'>
                            <h2>✅ Congratulations!</h2>
                            <p>We are pleased to inform you that the enrollment application for <strong>{enrollment.StudentName}</strong> has been successfully approved and enrolled!</p>
                        </div>

                        <div class='details-section'>
                            <h3>📋 Enrollment Details</h3>
                            
                            <div class='detail-grid'>
                                <div class='detail-item'>
                                    <strong>Student Name</strong>
                                    <span>{enrollment.StudentName}</span>
                                </div>
                                <div class='detail-item'>
                                    <strong>Grade Level</strong>
                                    <span>Grade {enrollment.GradeLevel}</span>
                                </div>
                                <div class='detail-item'>
                                    <strong>Section</strong>
                                    <span>Section {enrollment.Section}</span>
                                </div>
                                <div class='detail-item'>
                                    <strong>Assigned Room</strong>
                                    <span>{assignedRoom}</span>
                                </div>
                                {professorsHtml}
                            </div>
                        </div>

                        <div class='schedule-box'>
                            <h3>🕐 Class Schedule</h3>
                            <div class='time'>{classSchedule}</div>
                            <div class='shift'>{shift} Shift</div>
                            <p style='margin: 15px 0 0; opacity: 0.95; font-size: 14px;'>Monday to Friday | 5 Hours Daily</p>
                        </div>

                        <div class='important-info'>
                            <h3>⚠️ Important Reminders</h3>
                            <ul>
                                <li><strong>First Day of Class:</strong> Please check the school calendar for the start date.</li>
                                <li><strong>Required Items:</strong> School uniform, supplies, and ID requirements will be sent separately.</li>
                                <li><strong>Orientation:</strong> Watch for updates about the parent-student orientation schedule.</li>
                            </ul>
                        </div>

                        <div class='contact-section'>
                            <h3>📞 Need Help?</h3>
                            <div class='contact-info'>
                                <p>If you have any questions or concerns, please don't hesitate to contact us:</p>
                                <p><strong>📞 Phone:</strong> (02) 239 8307</p>
                                <p><strong>✉️ Email:</strong> fortbonifacio01@gmail.com</p>
                            </div>
                        </div>
                    </div>
                    
                    <div class='footer'>
                        <p style='margin: 0;'>&copy; {DateTime.Now.Year} Elementary School. All Rights Reserved.</p>
                    </div>
                </div>
            </body>
            </html>";

                await _emailService.SendEmailAsync(enrollment.User.Email, subject, htmlMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send enrollment email");
            }

            TempData["Success"] = $"{enrollment.StudentName} has been successfully enrolled with subject assignments!";
            return RedirectToAction(nameof(ViewStudents));
        }

        // Decline enrollment
        [HttpPost]
        public async Task<IActionResult> DeclineEnrollment(int id)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment == null)
            {
                return NotFound();
            }

            enrollment.Status = "rejected";
            await _context.SaveChangesAsync();

            // Send rejection email
            try
            {
                string subject = "Enrollment Application Update - Elementary School";
                string htmlMessage = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; padding: 20px;'>
                        <div style='max-width: 600px; margin: 0 auto; background: white; border-radius: 10px; padding: 30px;'>
                            <h2 style='color: #dc3545; text-align: center;'>Enrollment Application Update</h2>
                            
                            <p>Dear Parent/Guardian,</p>
                            
                            <p>We regret to inform you that the enrollment application for <strong>{enrollment.StudentName}</strong> could not be approved at this time.</p>
                            
                            <p>For more information or to resubmit your application, please contact our enrollment office.</p>
                            
                            <p style='margin-top: 30px; color: #666;'>
                                Contact us at:<br>
                                📞 (02) 239 8307<br>
                                ✉️ fortbonifacio01@gmail.com
                            </p>
                        </div>
                    </body>
                    </html>";

                await _emailService.SendEmailAsync(enrollment.User.Email, subject, htmlMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send rejection email");
            }

            TempData["Info"] = $"Enrollment for {enrollment.StudentName} has been declined.";
            return RedirectToAction(nameof(ViewStudents));
        }

        // Professor Management
        public async Task<IActionResult> ManageProfessors()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var professors = await _context.Users
                .Where(u => u.Role == "professor")
                .OrderBy(u => u.AssignedGradeLevel)
                .ThenBy(u => u.AssignedSection)
                .ToListAsync();

            // Get section assignment counts for each professor
            var professorIds = professors.Select(p => p.Id).ToList();
            var assignmentCounts = await _context.ProfessorSectionAssignments
                .Where(a => professorIds.Contains(a.ProfessorId))
                .GroupBy(a => a.ProfessorId)
                .Select(g => new { ProfessorId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ProfessorId, x => x.Count);

            ViewBag.AssignmentCounts = assignmentCounts;

            return View(professors);
        }

        // Add Professor - GET
        public IActionResult AddProfessor()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // Add Professor - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProfessor(AddProfessorViewModel model)
        {
            if (ModelState.IsValid)
            {
                int gradeLevel = int.Parse(model.GradeLevel);

                // Grades 1-3: Only one professor per grade (no section, no subject)
                if (gradeLevel >= 1 && gradeLevel <= 3)
                {
                    // Check if grade already has a professor (in Users table)
                    var existingProfessor = await _context.Users
                        .FirstOrDefaultAsync(u => u.Role == "professor" &&
                                                 u.AssignedGradeLevel == model.GradeLevel);

                    if (existingProfessor != null)
                    {
                        ModelState.AddModelError("GradeLevel",
                            $"Grade {model.GradeLevel} already has an assigned professor: {existingProfessor.FullName}. Only ONE professor is allowed for grades 1-3 (they handle all sections and subjects).");
                        return View(model);
                    }

                    // Also check ProfessorSectionAssignments table
                    var existingAssignment = await _context.ProfessorSectionAssignments
                        .FirstOrDefaultAsync(a => a.GradeLevel == model.GradeLevel);

                    if (existingAssignment != null)
                    {
                        var assignedProf = await _context.Users.FindAsync(existingAssignment.ProfessorId);
                        ModelState.AddModelError("GradeLevel",
                            $"Grade {model.GradeLevel} already has an assigned professor: {assignedProf?.FullName}. Only ONE professor is allowed for grades 1-3.");
                        return View(model);
                    }

                    // For grades 1-3, section and subject are not needed
                    model.Section = null;
                    model.Subject = null;
                }
                // Grades 4-6: Up to 6 professors per section, each with different subjects
                else if (gradeLevel >= 4 && gradeLevel <= 6)
                {
                    // Validate section is provided
                    if (!model.Section.HasValue)
                    {
                        ModelState.AddModelError("Section", "Section is required for grades 4-6.");
                        return View(model);
                    }

                    // Validate subject is provided
                    if (string.IsNullOrEmpty(model.Subject))
                    {
                        ModelState.AddModelError("Subject", "Subject is required for grades 4-6.");
                        return View(model);
                    }

                    // NEW: Check if this professor's email already exists with a different subject assignment
                    var existingProfByEmail = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == model.Email && u.Role == "professor");

                    if (existingProfByEmail != null)
                    {
                        // Professor already exists - check if they're trying to assign a different subject
                        if (!string.IsNullOrEmpty(existingProfByEmail.AssignedSubject) &&
                            existingProfByEmail.AssignedSubject != model.Subject)
                        {
                            ModelState.AddModelError("Email",
                                $"This professor is already assigned to teach {existingProfByEmail.AssignedSubject}. A professor can only teach ONE subject across all grades and sections.");
                            return View(model);
                        }

                        // Check in ProfessorSectionAssignments for different subjects
                        var existingAssignmentWithDifferentSubject = await _context.ProfessorSectionAssignments
                            .FirstOrDefaultAsync(a => a.ProfessorId == existingProfByEmail.Id &&
                                                     a.Subject != model.Subject);

                        if (existingAssignmentWithDifferentSubject != null)
                        {
                            ModelState.AddModelError("Subject",
                                $"This professor is already assigned to teach {existingAssignmentWithDifferentSubject.Subject}. A professor can only teach ONE subject across all grades and sections.");
                            return View(model);
                        }
                    }

                    // Check if this section already has a professor for this subject
                    var existingSubjectProfessor = await _context.Users
                        .FirstOrDefaultAsync(u => u.Role == "professor" &&
                                                 u.AssignedGradeLevel == model.GradeLevel &&
                                                 u.AssignedSection == model.Section &&
                                                 u.AssignedSubject == model.Subject);

                    if (existingSubjectProfessor != null)
                    {
                        ModelState.AddModelError("Subject",
                            $"Grade {model.GradeLevel} - Section {model.Section} already has a professor for {model.Subject}: {existingSubjectProfessor.FullName}");
                        return View(model);
                    }

                    // Check how many professors are already assigned to this section
                    var professorsInSection = await _context.Users
                        .CountAsync(u => u.Role == "professor" &&
                                        u.AssignedGradeLevel == model.GradeLevel &&
                                        u.AssignedSection == model.Section);

                    if (professorsInSection >= 6)
                    {
                        ModelState.AddModelError("Section",
                            $"Grade {model.GradeLevel} - Section {model.Section} already has 6 professors (maximum allowed).");
                        return View(model);
                    }
                }

                // Get room assignment
                string assignedRoom = model.Section.HasValue
                    ? RoomAssignmentHelper.GetRoomForSection(model.GradeLevel, model.Section.Value)
                    : null;

                var user = new Users
                {
                    FullName = model.FullName,
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true,
                    Role = "professor",
                    AssignedGradeLevel = model.GradeLevel,
                    AssignedSection = model.Section,
                    AssignedSubject = model.Subject,
                    AssignedRoom = assignedRoom
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    string message = gradeLevel >= 1 && gradeLevel <= 3
                        ? $"Professor {model.FullName} has been added successfully for Grade {model.GradeLevel}."
                        : $"Professor {model.FullName} has been added successfully for Grade {model.GradeLevel} - Section {model.Section} ({model.Subject}).";

                    TempData["Success"] = message;
                    return RedirectToAction(nameof(ManageProfessors));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // Edit Professor - GET
        public async Task<IActionResult> EditProfessor(string id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var professor = await _userManager.FindByIdAsync(id);
            if (professor == null || professor.Role != "professor")
            {
                return NotFound();
            }

            var model = new EditProfessorViewModel
            {
                Id = professor.Id,
                FullName = professor.FullName,
                Email = professor.Email,
                GradeLevel = professor.AssignedGradeLevel,
                Section = professor.AssignedSection,
                Subject = professor.AssignedSubject
            };

            return View(model);
        }

        // Edit Professor - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfessor(EditProfessorViewModel model)
        {
            if (ModelState.IsValid)
            {
                var professor = await _userManager.FindByIdAsync(model.Id);
                if (professor == null)
                {
                    return NotFound();
                }

                int gradeLevel = int.Parse(model.GradeLevel);

                // Grades 1-3: Only one professor per grade (no section, no subject)
                if (gradeLevel >= 1 && gradeLevel <= 3)
                {
                    // Check if grade already has another professor (in Users table)
                    var existingProfessor = await _context.Users
                        .FirstOrDefaultAsync(u => u.Role == "professor" &&
                                                 u.AssignedGradeLevel == model.GradeLevel &&
                                                 u.Id != model.Id);

                    if (existingProfessor != null)
                    {
                        ModelState.AddModelError("GradeLevel",
                            $"Grade {model.GradeLevel} already has an assigned professor: {existingProfessor.FullName}. Only ONE professor is allowed for grades 1-3 (they handle all sections and subjects).");
                        return View(model);
                    }

                    // Check ProfessorSectionAssignments table
                    var existingAssignment = await _context.ProfessorSectionAssignments
                        .FirstOrDefaultAsync(a => a.GradeLevel == model.GradeLevel &&
                                                 a.ProfessorId != model.Id);

                    if (existingAssignment != null)
                    {
                        var assignedProf = await _context.Users.FindAsync(existingAssignment.ProfessorId);
                        ModelState.AddModelError("GradeLevel",
                            $"Grade {model.GradeLevel} already has an assigned professor: {assignedProf?.FullName}. Only ONE professor is allowed for grades 1-3.");
                        return View(model);
                    }

                    // For grades 1-3, section and subject are not needed
                    model.Section = null;
                    model.Subject = null;
                }
                // Grades 4-6: Up to 6 professors per section, each with different subjects
                // Grades 4-6: Up to 6 professors per section, each with different subjects
                else if (gradeLevel >= 4 && gradeLevel <= 6)
                {
                    // Validate section is provided
                    if (!model.Section.HasValue)
                    {
                        ModelState.AddModelError("Section", "Section is required for grades 4-6.");
                        return View(model);
                    }

                    // Validate subject is provided
                    if (string.IsNullOrEmpty(model.Subject))
                    {
                        ModelState.AddModelError("Subject", "Subject is required for grades 4-6.");
                        return View(model);
                    }

                    // NEW: Check if professor is trying to change to a different subject
                    var currentSubject = professor.AssignedSubject;
                    var allProfessorAssignments = await _context.ProfessorSectionAssignments
                        .Where(a => a.ProfessorId == model.Id)
                        .Select(a => a.Subject)
                        .ToListAsync();

                    // If professor has existing assignments, check if they're trying to change subject
                    if (!string.IsNullOrEmpty(currentSubject) && currentSubject != model.Subject)
                    {
                        ModelState.AddModelError("Subject",
                            $"This professor is already assigned to teach {currentSubject}. A professor can only teach ONE subject. If you want to change their subject, you must first remove all their current assignments.");
                        return View(model);
                    }

                    if (allProfessorAssignments.Any() && allProfessorAssignments.Any(s => s != model.Subject))
                    {
                        var existingSubject = allProfessorAssignments.First(s => s != model.Subject);
                        ModelState.AddModelError("Subject",
                            $"This professor is already assigned to teach {existingSubject} in other sections. A professor can only teach ONE subject. If you want to change their subject, you must first remove all their current assignments.");
                        return View(model);
                    }

                    // Check if this section already has another professor for this subject
                    var existingSubjectProfessor = await _context.Users
                        .FirstOrDefaultAsync(u => u.Role == "professor" &&
                                                 u.AssignedGradeLevel == model.GradeLevel &&
                                                 u.AssignedSection == model.Section &&
                                                 u.AssignedSubject == model.Subject &&
                                                 u.Id != model.Id);

                    if (existingSubjectProfessor != null)
                    {
                        ModelState.AddModelError("Subject",
                            $"Grade {model.GradeLevel} - Section {model.Section} already has a professor for {model.Subject}: {existingSubjectProfessor.FullName}");
                        return View(model);
                    }

                    // Check how many professors are already assigned to this section (excluding current professor)
                    var professorsInSection = await _context.Users
                        .CountAsync(u => u.Role == "professor" &&
                                        u.AssignedGradeLevel == model.GradeLevel &&
                                        u.AssignedSection == model.Section &&
                                        u.Id != model.Id);

                    // If changing section, check if new section has space
                    if (professor.AssignedSection != model.Section && professorsInSection >= 6)
                    {
                        ModelState.AddModelError("Section",
                            $"Grade {model.GradeLevel} - Section {model.Section} already has 6 professors (maximum allowed).");
                        return View(model);
                    }
                }

                // Update room assignment
                string assignedRoom = model.Section.HasValue
                    ? RoomAssignmentHelper.GetRoomForSection(model.GradeLevel, model.Section.Value)
                    : null;

                professor.FullName = model.FullName;
                professor.Email = model.Email;
                professor.UserName = model.Email;
                professor.AssignedGradeLevel = model.GradeLevel;
                professor.AssignedSection = model.Section;
                professor.AssignedSubject = model.Subject;
                professor.AssignedRoom = assignedRoom;

                var result = await _userManager.UpdateAsync(professor);

                if (result.Succeeded)
                {
                    TempData["Success"] = $"Professor {model.FullName} has been updated successfully.";
                    return RedirectToAction(nameof(ManageProfessors));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        // Delete Professor
        [HttpPost]
        public async Task<IActionResult> DeleteProfessor(string id)
        {
            var professor = await _userManager.FindByIdAsync(id);
            if (professor == null || professor.Role != "professor")
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(professor);

            if (result.Succeeded)
            {
                TempData["Success"] = $"Professor {professor.FullName} has been deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to delete professor.";
            }

            return RedirectToAction(nameof(ManageProfessors));
        }

        // Manage Professor Sections - View all sections assigned to a professor
        public async Task<IActionResult> ManageProfessorSections(string id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var professor = await _userManager.FindByIdAsync(id);
            if (professor == null || professor.Role != "professor")
            {
                return NotFound();
            }

            // Get all section assignments for this professor
            var assignments = await _context.ProfessorSectionAssignments
                .Where(a => a.ProfessorId == id)
                .OrderBy(a => a.GradeLevel)
                .ThenBy(a => a.Section)
                .ToListAsync();

            // Also include the primary assignment from the Users table if it exists
            if (professor.AssignedGradeLevel != null && professor.AssignedSection.HasValue)
            {
                var primaryExists = assignments.Any(a =>
                    a.GradeLevel == professor.AssignedGradeLevel &&
                    a.Section == professor.AssignedSection.Value);

                if (!primaryExists)
                {
                    assignments.Insert(0, new ProfessorSectionAssignment
                    {
                        Id = 0, // Temporary ID
                        ProfessorId = professor.Id,
                        GradeLevel = professor.AssignedGradeLevel,
                        Section = professor.AssignedSection.Value,
                        Subject = professor.AssignedSubject,
                        AssignedRoom = professor.AssignedRoom
                    });
                }
            }

            ViewBag.Professor = professor;
            ViewBag.Assignments = assignments;
            ViewBag.GradeLevel = professor.AssignedGradeLevel;

            return View();
        }

        private async Task<bool> HasTimeConflict(string gradeLevel, int section, string startTime, string endTime, string dayOfWeek, string professorId = null)
        {
            if (string.IsNullOrEmpty(startTime) || string.IsNullOrEmpty(endTime) || string.IsNullOrEmpty(dayOfWeek))
            {
                return false; // No time specified, no conflict checking needed
            }

            // Get all assignments for this grade and section
            var existingAssignments = await _context.ProfessorSectionAssignments
                .Where(a => a.GradeLevel == gradeLevel &&
                           a.Section == section &&
                           a.StartTime != null &&
                           a.EndTime != null &&
                           (professorId == null || a.ProfessorId != professorId))
                .ToListAsync();

            var newStart = TimeSpan.Parse(startTime);
            var newEnd = TimeSpan.Parse(endTime);

            foreach (var assignment in existingAssignments)
            {
                // Check if days overlap
                if (!string.IsNullOrEmpty(assignment.DayOfWeek))
                {
                    var existingDays = assignment.DayOfWeek.Split(',').Select(d => d.Trim()).ToList();
                    var newDays = dayOfWeek.Split(',').Select(d => d.Trim()).ToList();

                    bool daysOverlap = existingDays.Any(d => newDays.Contains(d));

                    if (daysOverlap)
                    {
                        var existingStart = TimeSpan.Parse(assignment.StartTime);
                        var existingEnd = TimeSpan.Parse(assignment.EndTime);

                        // Check if times overlap
                        bool timesOverlap = (newStart < existingEnd && newEnd > existingStart);

                        if (timesOverlap)
                        {
                            return true; // Conflict found
                        }
                    }
                }
            }

            return false; // No conflict
        }

        // Add Section to Professor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSectionToProfessor(
    string professorId,
    string gradeLevel,
    int section,
    string? subject,
    string? startTime,
    string? endTime,
    string? dayOfWeek)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var professor = await _userManager.FindByIdAsync(professorId);
            if (professor == null || professor.Role != "professor")
            {
                return NotFound();
            }

            int grade = int.Parse(gradeLevel);

            if (grade >= 1 && grade <= 3)
            {
                TempData["Error"] = $"Cannot add additional sections for Grade {gradeLevel}. Grades 1-3 can only have ONE professor who handles ALL sections and subjects for that grade.";
                return RedirectToAction(nameof(ManageProfessorSections), new { id = professorId });
            }

            // Validate time fields - if any time field is filled, all must be filled
            bool hasAnyTimeField = !string.IsNullOrEmpty(startTime) || !string.IsNullOrEmpty(endTime) || !string.IsNullOrEmpty(dayOfWeek);
            bool hasAllTimeFields = !string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime) && !string.IsNullOrEmpty(dayOfWeek);

            if (hasAnyTimeField && !hasAllTimeFields)
            {
                TempData["Error"] = "If you specify a schedule, you must fill in all time fields (Day, Start Time, and End Time).";
                return RedirectToAction(nameof(ManageProfessorSections), new { id = professorId });
            }

            // Validate that end time is after start time
            if (!string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime))
            {
                try
                {
                    var start = TimeSpan.Parse(startTime);
                    var end = TimeSpan.Parse(endTime);

                    if (start >= end)
                    {
                        TempData["Error"] = "End time must be after start time.";
                        return RedirectToAction(nameof(ManageProfessorSections), new { id = professorId });
                    }
                }
                catch
                {
                    TempData["Error"] = "Invalid time format. Please use HH:mm format (e.g., 08:00).";
                    return RedirectToAction(nameof(ManageProfessorSections), new { id = professorId });
                }
            }

            // Check for time conflicts
            if (hasAllTimeFields)
            {
                bool hasConflict = await HasTimeConflict(gradeLevel, section, startTime, endTime, dayOfWeek, professorId);
                if (hasConflict)
                {
                    TempData["Error"] = $"⚠️ Time conflict detected! Another professor already has a class scheduled for Grade {gradeLevel} - Section {section} during this time period ({dayOfWeek}: {startTime} - {endTime}).";
                    return RedirectToAction(nameof(ManageProfessorSections), new { id = professorId });
                }
            }

            // Check if assignment already exists in ProfessorSectionAssignments
            var existing = await _context.ProfessorSectionAssignments
                .FirstOrDefaultAsync(a => a.ProfessorId == professorId &&
                                         a.GradeLevel == gradeLevel &&
                                         a.Section == section);

            if (existing != null)
            {
                TempData["Error"] = $"Professor is already assigned to Grade {gradeLevel} - Section {section}.";
                return RedirectToAction(nameof(ManageProfessorSections), new { id = professorId });
            }

            // Check if this is the professor's primary assignment
            if (professor.AssignedGradeLevel == gradeLevel && professor.AssignedSection == section)
            {
                TempData["Error"] = $"This is already the professor's primary assignment. Use Edit to change it.";
                return RedirectToAction(nameof(ManageProfessorSections), new { id = professorId });
            }

            // For grades 4-6, check subject conflicts
            // For grades 4-6, check subject conflicts
            if (grade >= 4 && grade <= 6)
            {
                if (string.IsNullOrEmpty(subject))
                {
                    TempData["Error"] = "Subject is required for grades 4-6.";
                    return RedirectToAction(nameof(ManageProfessorSections), new { id = professorId });
                }

                // NEW: Check if professor is trying to teach a different subject
                var professorCurrentSubject = professor.AssignedSubject;
                var allProfessorAssignments = await _context.ProfessorSectionAssignments
                    .Where(a => a.ProfessorId == professorId)
                    .Select(a => a.Subject)
                    .ToListAsync();

                // If professor has a primary subject, new assignment must be the same subject
                if (!string.IsNullOrEmpty(professorCurrentSubject) && professorCurrentSubject != subject)
                {
                    TempData["Error"] = $"This professor is already assigned to teach {professorCurrentSubject}. A professor can only teach ONE subject across all grades and sections.";
                    return RedirectToAction(nameof(ManageProfessorSections), new { id = professorId });
                }

                // If professor has other assignments, check they're all the same subject
                if (allProfessorAssignments.Any() && allProfessorAssignments.Any(s => s != subject))
                {
                    var existingSubject = allProfessorAssignments.First(s => s != subject);
                    TempData["Error"] = $"This professor is already assigned to teach {existingSubject} in other sections. A professor can only teach ONE subject across all grades and sections.";
                    return RedirectToAction(nameof(ManageProfessorSections), new { id = professorId });
                }

                // Check if another professor already teaches this subject in this section (in assignments table)
               // Check if another professor already teaches this subject in this section (in assignments table)
var subjectConflictInAssignments = await _context.ProfessorSectionAssignments
    .AnyAsync(a => a.GradeLevel == gradeLevel &&
                 a.Section == section &&
                 a.Subject == subject &&
                 a.ProfessorId != professorId);

// Also check in Users table (primary assignments)
var subjectConflictInUsers = await _context.Users
    .AnyAsync(u => u.Role == "professor" &&
                 u.AssignedGradeLevel == gradeLevel &&
                 u.AssignedSection == section &&
                 u.AssignedSubject == subject &&
                 u.Id != professorId);

if (subjectConflictInAssignments || subjectConflictInUsers)
{
    TempData["Error"] = $"Another professor is already assigned to Grade {gradeLevel} - Section {section} for {subject}.";
    return RedirectToAction(nameof(ManageProfessorSections), new { id = professorId });
}
            }

            // Get room assignment
            string assignedRoom = RoomAssignmentHelper.GetRoomForSection(gradeLevel, section);

            var assignment = new ProfessorSectionAssignment
            {
                ProfessorId = professorId,
                GradeLevel = gradeLevel,
                Section = section,
                Subject = subject,
                AssignedRoom = assignedRoom,
                StartTime = startTime,
                EndTime = endTime,
                DayOfWeek = dayOfWeek
            };

            _context.ProfessorSectionAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            string scheduleInfo = hasAllTimeFields
                ? $" ({dayOfWeek}: {startTime} - {endTime})"
                : "";

            TempData["Success"] = $"Professor has been assigned to Grade {gradeLevel} - Section {section}{scheduleInfo}.";
            return RedirectToAction(nameof(ManageProfessorSections), new { id = professorId });
        }

        // Remove Section from Professor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSectionFromProfessor(int assignmentId, string professorId)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var assignment = await _context.ProfessorSectionAssignments.FindAsync(assignmentId);
            if (assignment == null || assignment.ProfessorId != professorId)
            {
                return NotFound();
            }

            _context.ProfessorSectionAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Section assignment removed successfully.";
            return RedirectToAction(nameof(ManageProfessorSections), new { id = professorId });
        }

        // Section Management
        public async Task<IActionResult> ManageSections()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new SectionManagementViewModel();

            // Get all grade levels (1-6)
            for (int grade = 1; grade <= 6; grade++)
            {
                var gradeLevel = new GradeLevelSections
                {
                    GradeLevel = grade.ToString()
                };

                // Get students grouped by section for this grade
                var studentsInGrade = await _context.Enrollments
                    .Where(e => e.GradeLevel == grade.ToString() && e.Status == "approved")
                    .GroupBy(e => e.Section)
                    .Select(g => new SectionInfo
                    {
                        SectionNumber = g.Key ?? 0,
                        StudentCount = g.Count()
                    })
                    .ToListAsync();

                gradeLevel.Sections = studentsInGrade;
                gradeLevel.TotalStudents = studentsInGrade.Sum(s => s.StudentCount);

                viewModel.GradeLevels.Add(gradeLevel);
            }

            return View(viewModel);
        }

        // View Students in Specific Section
        public async Task<IActionResult> ViewSectionStudents(string gradeLevel, int section)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var students = await _context.Enrollments
                .Include(e => e.User)
                .Where(e => e.GradeLevel == gradeLevel && e.Section == section && e.Status == "approved")
                .OrderBy(e => e.StudentName)
                .ToListAsync();

            ViewBag.GradeLevel = gradeLevel;
            ViewBag.Section = section;
            ViewBag.StudentCount = students.Count;

            return View(students);
        }

        // Class Schedules
        public async Task<IActionResult> ClassSchedules()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var schedules = new List<ClassScheduleViewModel>();

            for (int grade = 1; grade <= 6; grade++)
            {
                var gradeLevel = grade.ToString();
                var gradeInt = grade;
                bool isMorningShift = gradeInt % 2 == 0; // Grades 2, 4, 6 = Morning
                string shift = isMorningShift ? "Morning" : "Afternoon";
                string schedule = isMorningShift ? "7:00 AM - 12:00 PM" : "1:00 PM - 6:00 PM";
                string startTime = isMorningShift ? "7:00 AM" : "1:00 PM";
                string endTime = isMorningShift ? "12:00 PM" : "6:00 PM";

                var scheduleViewModel = new ClassScheduleViewModel
                {
                    GradeLevel = gradeLevel,
                    Schedule = schedule,
                    StartTime = startTime,
                    EndTime = endTime,
                    Shift = shift,
                    Professors = new List<ProfessorScheduleInfo>()
                };

                // Get professors for this grade level
                var professors = await _context.Users
                    .Where(u => u.Role == "professor" && u.AssignedGradeLevel == gradeLevel)
                    .OrderBy(u => u.AssignedSection)
                    .ThenBy(u => u.AssignedSubject)
                    .ToListAsync();

                foreach (var professor in professors)
                {
                    scheduleViewModel.Professors.Add(new ProfessorScheduleInfo
                    {
                        ProfessorName = professor.FullName,
                        Subject = professor.AssignedSubject ?? "All Subjects",
                        Section = professor.AssignedSection,
                        Room = professor.AssignedRoom ?? "TBA",
                        TimeSlot = schedule
                    });
                }

                schedules.Add(scheduleViewModel);
            }

            return View(schedules);
        }

        // Remove Student from Section (not delete enrollment)
        [HttpPost]
        public async Task<IActionResult> RemoveStudent(int id)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
            {
                TempData["Error"] = "Student enrollment not found.";
                return RedirectToAction(nameof(ViewStudents));
            }

            var gradeLevel = enrollment.GradeLevel;
            var section = enrollment.Section;
            var studentName = enrollment.StudentName;

            // Remove student from section (set section to null)
            // Student remains enrolled but needs to be reassigned to a section
            enrollment.Section = null;
            enrollment.Status = "pending";

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Student {studentName} has been removed from Grade {gradeLevel} - Section {section}. They can be reassigned to another section.";

            if (gradeLevel != null && section.HasValue)
            {
                return RedirectToAction(nameof(ViewSectionStudents), new { gradeLevel, section });
            }

            return RedirectToAction(nameof(ViewStudents));
        }

        // Reassign Student to Section - GET
        public async Task<IActionResult> ReassignStudent(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment == null)
            {
                TempData["Error"] = "Student enrollment not found.";
                return RedirectToAction(nameof(ViewStudents));
            }


            var gradeLevel = enrollment.GradeLevel;
            var sectionsWithCapacity = new List<int>();
            var sectionCapacityData = new Dictionary<int, int>();


            for (int i = 1; i <= 8; i++)
            {
                var studentsInSection = await _context.Enrollments
                    .CountAsync(e => e.GradeLevel == gradeLevel &&
                                   e.Section == i &&
                                   e.Status == "approved");

                sectionCapacityData[i] = studentsInSection;

                if (studentsInSection < 40)
                {
                    sectionsWithCapacity.Add(i);
                }
            }

            ViewBag.AvailableSections = sectionsWithCapacity;
            ViewBag.SectionCapacity = sectionCapacityData;
            ViewBag.GradeLevel = gradeLevel;

            return View(enrollment);
        }

        [HttpPost]
        public async Task<IActionResult> ReassignStudent(int id, int newSection)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
            {
                TempData["Error"] = "Student enrollment not found.";
                return RedirectToAction(nameof(ViewStudents));
            }


            var studentsInSection = await _context.Enrollments
                .CountAsync(e => e.GradeLevel == enrollment.GradeLevel &&
                               e.Section == newSection &&
                               e.Status == "approved");

            if (studentsInSection >= 40)
            {
                TempData["Error"] = $"Section {newSection} is full (40/40 students). Please choose another section.";
                return RedirectToAction(nameof(ReassignStudent), new { id });
            }

            var oldSection = enrollment.Section;
            enrollment.Section = newSection;
            enrollment.Status = "approved";
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Student {enrollment.StudentName} has been reassigned from Section {oldSection} to Section {newSection}.";

            return RedirectToAction(nameof(ViewSectionStudents), new { gradeLevel = enrollment.GradeLevel, section = newSection });
        }


        [HttpGet]
        public async Task<IActionResult> GetAvailableSections(string gradeLevel)
        {
            var takenSections = await _context.Users
                .Where(u => u.Role == "professor" &&
                           u.AssignedGradeLevel == gradeLevel &&
                           u.AssignedSection.HasValue)
                .Select(u => new
                {
                    section = u.AssignedSection.Value,
                    professorName = u.FullName
                })
                .ToListAsync();

            return Json(new { takenSections });
        }
    }
}