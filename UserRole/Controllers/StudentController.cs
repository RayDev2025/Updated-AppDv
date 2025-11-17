using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserRoles.Data;
using UserRoles.Models;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace UserRoles.Controllers
{
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public StudentController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Student/EnrollmentForm
        public async Task<IActionResult> EnrollmentForm()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var existingEnrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId);
            ViewBag.HasEnrollment = (existingEnrollment != null);

            if (existingEnrollment != null)
            {
                return RedirectToAction("ViewEnrollment");
            }

            return View();
        }

        // POST: Student/ProcessEnrollment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessEnrollment(Enrollment enrollment, IFormFile birthCertFile, IFormFile form137File)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Upload birth certificate
                var birthCertPath = await UploadFile(birthCertFile, "birth_certificates");
                if (birthCertPath == null)
                {
                    TempData["Error"] = "Birth Certificate upload failed. Only PDF files under 5MB allowed.";
                    return RedirectToAction("EnrollmentForm");
                }

                // Upload form137
                var form137Path = await UploadFile(form137File, "form137");
                if (form137Path == null)
                {
                    TempData["Error"] = "Form 137 upload failed. Only PDF files under 5MB allowed.";
                    return RedirectToAction("EnrollmentForm");
                }

                // Assign section
                var section = await AssignSection(enrollment.GradeLevel);

                // Save enrollment
                enrollment.UserId = userId;
                enrollment.BirthCertificate = birthCertPath;
                enrollment.Form137 = form137Path;
                enrollment.Section = section;
                enrollment.EnrollmentDate = DateTime.Now;
                enrollment.Status = "pending";

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Enrollment submitted successfully! You are assigned to Section {section}.";
                return RedirectToAction("ViewEnrollment");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("EnrollmentForm");
            }
        }

        // ==================== NEW: EDIT ENROLLMENT ====================

        // GET: Student/EditEnrollment
        public async Task<IActionResult> EditEnrollment()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId);

            if (enrollment == null)
            {
                return RedirectToAction("EnrollmentForm");
            }

            // Only allow editing if status is pending
            if (enrollment.Status != "pending")
            {
                TempData["Error"] = "You cannot edit your enrollment after it has been approved or rejected.";
                return RedirectToAction("ViewEnrollment");
            }

            ViewBag.HasEnrollment = true;
            return View(enrollment);
        }

        // POST: Student/UpdateEnrollment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEnrollment(Enrollment enrollment, IFormFile birthCertFile, IFormFile form137File)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var existingEnrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId);

                if (existingEnrollment == null)
                {
                    TempData["Error"] = "Enrollment not found.";
                    return RedirectToAction("EnrollmentForm");
                }

                // Check if status is pending
                if (existingEnrollment.Status != "pending")
                {
                    TempData["Error"] = "You cannot edit your enrollment after it has been approved or rejected.";
                    return RedirectToAction("ViewEnrollment");
                }

                // Update basic information
                existingEnrollment.StudentName = enrollment.StudentName;
                existingEnrollment.ParentName = enrollment.ParentName;
                existingEnrollment.ContactNumber = enrollment.ContactNumber;
                existingEnrollment.Address = enrollment.Address;

                // Check if grade level changed - if yes, reassign section
                if (existingEnrollment.GradeLevel != enrollment.GradeLevel)
                {
                    // Decrease count in old grade level
                    var oldCapacity = await _context.SectionCapacities
                        .FirstOrDefaultAsync(sc => sc.GradeLevel == existingEnrollment.GradeLevel);
                    if (oldCapacity != null && oldCapacity.StudentsInCurrentSection > 0)
                    {
                        oldCapacity.StudentsInCurrentSection--;
                    }

                    // Assign new section for new grade level
                    var newSection = await AssignSection(enrollment.GradeLevel);
                    existingEnrollment.GradeLevel = enrollment.GradeLevel;
                    existingEnrollment.Section = newSection;
                }

                // Update birth certificate if new file uploaded
                if (birthCertFile != null && birthCertFile.Length > 0)
                {
                    // Delete old file
                    DeleteFile(existingEnrollment.BirthCertificate);

                    var birthCertPath = await UploadFile(birthCertFile, "birth_certificates");
                    if (birthCertPath == null)
                    {
                        TempData["Error"] = "Birth Certificate upload failed. Only PDF files under 5MB allowed.";
                        return RedirectToAction("EditEnrollment");
                    }
                    existingEnrollment.BirthCertificate = birthCertPath;
                }

                // Update form 137 if new file uploaded
                if (form137File != null && form137File.Length > 0)
                {
                    // Delete old file
                    DeleteFile(existingEnrollment.Form137);

                    var form137Path = await UploadFile(form137File, "form137");
                    if (form137Path == null)
                    {
                        TempData["Error"] = "Form 137 upload failed. Only PDF files under 5MB allowed.";
                        return RedirectToAction("EditEnrollment");
                    }
                    existingEnrollment.Form137 = form137Path;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Enrollment updated successfully!";
                return RedirectToAction("ViewEnrollment");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("EditEnrollment");
            }
        }

        // ==================== NEW: RESET ENROLLMENT ====================

        // POST: Student/ResetEnrollment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetEnrollment()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId);

                if (enrollment == null)
                {
                    TempData["Error"] = "Enrollment not found.";
                    return RedirectToAction("EnrollmentForm");
                }

                // Only allow reset if status is pending
                if (enrollment.Status != "pending")
                {
                    TempData["Error"] = "You cannot reset your enrollment after it has been approved or rejected.";
                    return RedirectToAction("ViewEnrollment");
                }

                // Delete uploaded files
                DeleteFile(enrollment.BirthCertificate);
                DeleteFile(enrollment.Form137);

                // Decrease section capacity count
                var capacity = await _context.SectionCapacities
                    .FirstOrDefaultAsync(sc => sc.GradeLevel == enrollment.GradeLevel);
                if (capacity != null && capacity.StudentsInCurrentSection > 0)
                {
                    capacity.StudentsInCurrentSection--;
                }

                // Delete enrollment record
                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Enrollment reset successfully! You can now create a new enrollment.";
                return RedirectToAction("EnrollmentForm");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("ViewEnrollment");
            }
        }

        // ==================== EXISTING METHODS ====================

        // GET: Student/ViewEnrollment
        public async Task<IActionResult> ViewEnrollment()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId);
            ViewBag.HasEnrollment = (enrollment != null);

            if (enrollment == null)
            {
                return RedirectToAction("EnrollmentForm");
            }

            return View(enrollment);
        }

        // GET: Student/PrintEnrollment
        public async Task<IActionResult> PrintEnrollment()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.UserId == userId);
            ViewBag.HasEnrollment = (enrollment != null);

            if (enrollment == null)
            {
                return RedirectToAction("EnrollmentForm");
            }

            return View(enrollment);
        }

        public async Task<IActionResult> EnrollmentProgress()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var enrollment = await _context.Enrollments.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == userId);
            ViewBag.HasEnrollment = (enrollment != null);

            if (enrollment == null)
            {
                return RedirectToAction("EnrollmentForm");
            }

            return View(enrollment);
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        // ==================== HELPER METHODS ====================

        // Helper method to upload files
        private async Task<string> UploadFile(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                return null;

            // Check file size (5MB max)
            if (file.Length > 5 * 1024 * 1024)
                return null;

            // Check file type (PDF only)
            if (file.ContentType != "application/pdf")
                return null;

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{folder}/{uniqueFileName}";
        }

        // NEW: Helper method to delete files
        private void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - file deletion is not critical
                Console.WriteLine($"Error deleting file: {ex.Message}");
            }
        }

        // Helper method to assign section
        private async Task<int> AssignSection(string gradeLevel)
        {
            var capacity = await _context.SectionCapacities
                .FirstOrDefaultAsync(sc => sc.GradeLevel == gradeLevel);

            if (capacity == null)
            {
                // Create new capacity record
                capacity = new SectionCapacity
                {
                    GradeLevel = gradeLevel,
                    CurrentSection = 1,
                    StudentsInCurrentSection = 0
                };
                _context.SectionCapacities.Add(capacity);
            }

            // Check if current section is full
            if (capacity.StudentsInCurrentSection >= 35)
            {
                capacity.CurrentSection++;
                capacity.StudentsInCurrentSection = 0;
            }

            capacity.StudentsInCurrentSection++;
            await _context.SaveChangesAsync();

            return capacity.CurrentSection;
        }
    }
}