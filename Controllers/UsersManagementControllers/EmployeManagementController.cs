using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Back_HR.Models;
using Back_HR.DTOs;
using System.ComponentModel.DataAnnotations;
using Back_HR.Models.enums;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using System.IO;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RHOnly")]
public class EmployeManagementController : ControllerBase
{
    private readonly HRContext _context;
    private readonly UserManager<User> _userManager;
    private readonly SmtpSettings _smtpSettings;

    public EmployeManagementController(HRContext context, UserManager<User> userManager, IOptions<SmtpSettings> smtpSettings)
    {
        _context = context;
        _userManager = userManager;
        _smtpSettings = smtpSettings.Value;
    }

    // CREATE: Add a new employee
    [HttpPost]
    public async Task<ActionResult<EmployeDTO>> PostEmployee([FromBody] CreateEmployeDTO createEmployeDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (await _userManager.FindByEmailAsync(createEmployeDto.Email) != null)
        {
            return BadRequest(new { Message = "Email is already in use." });
        }

        var employee = new Employe
        {
            Id = Guid.NewGuid(),
            Lastname = createEmployeDto.Lastname,
            Firstname = createEmployeDto.Firstname,
            Telephone = createEmployeDto.Telephone,
            Email = createEmployeDto.Email,
            UserName = createEmployeDto.Email,
            UserType = UserType.EMPLOYE,
            Poste = createEmployeDto.Poste,
            Department = createEmployeDto.Department,
            HireDate = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(employee, createEmployeDto.TemporaryPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        // Envoi de l'email de bienvenue
        try
        {
            await SendWelcomeEmail(employee, createEmployeDto.TemporaryPassword);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send welcome email: {ex.Message}");
        }

        var createdEmployeDto = new EmployeDTO
        {
            Id = employee.Id,
            Lastname = employee.Lastname,
            Firstname = employee.Firstname,
            Telephone = employee.Telephone,
            Email = employee.Email,
            Poste = employee.Poste,
            Department = employee.Department
        };

        return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, createdEmployeDto);
    }

    // READ: Get all employees
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeDTO>>> GetEmployees()
    {
        var employees = await _userManager.Users
            .OfType<Employe>()
            .Select(e => new EmployeDTO
            {
                Id = e.Id,
                Lastname = e.Lastname,
                Firstname = e.Firstname,
                Telephone = e.Telephone,
                Email = e.Email,
                Poste = e.Poste,
                Department = e.Department
            })
            .ToListAsync();
        return Ok(employees);
    }

    // READ: Get a single employee by ID
    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeDTO>> GetEmployee(Guid id)
    {
        var employee = await _userManager.Users
            .OfType<Employe>()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null) return NotFound();

        var employeDto = new EmployeDTO
        {
            Id = employee.Id,
            Lastname = employee.Lastname,
            Firstname = employee.Firstname,
            Telephone = employee.Telephone,
            Email = employee.Email,
            Department = employee.Department,
            Poste = employee.Poste
        };
        return Ok(employeDto);
    }

    // UPDATE: Modify an existing employee
    [HttpPut("{id}")]
    public async Task<IActionResult> PutEmployee(Guid id, [FromBody] UpdateEmployeDTO updateEmployeDto)
    {
        var employee = await _userManager.Users
            .OfType<Employe>()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null) return NotFound();

        employee.Lastname = updateEmployeDto.Lastname;
        employee.Firstname = updateEmployeDto.Firstname;
        employee.Telephone = updateEmployeDto.Telephone;
        employee.Poste = updateEmployeDto.Poste;
        employee.Department = updateEmployeDto.Department;

        // Only update email if it's different
        if (employee.Email != updateEmployeDto.Email)
        {
            employee.Email = updateEmployeDto.Email;
            employee.UserName = updateEmployeDto.Email;
            employee.NormalizedEmail = null;
            employee.NormalizedUserName = null;
        }

        var result = await _userManager.UpdateAsync(employee);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return NoContent();
    }

    // DELETE: Remove an employee
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(Guid id)
    {
        var employee = await _userManager.Users
            .OfType<Employe>()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null) return NotFound();

        var result = await _userManager.DeleteAsync(employee);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return NoContent();
    }

    private async Task SendWelcomeEmail(Employe employee, string temporaryPassword)
    {
        FileStream hrDocStream = null;
        FileStream presentationStream = null;

        try
        {
            Console.WriteLine($"Starting to send welcome email to {employee.Email}");

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
            emailMessage.To.Add(new MailboxAddress($"{employee.Firstname} {employee.Lastname}", employee.Email));
            emailMessage.Subject = "Welcome to Our Company!";
            Console.WriteLine("Email message created");

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                <h2>Welcome, {employee.Firstname} {employee.Lastname}!</h2>
                <p>We are thrilled to have you on board as part of our team. Your journey with us starts on {employee.HireDate:yyyy-MM-dd}.</p>
                <p>Please find attached the HR documents and a presentation about our company to help you get started.</p>
                <p>Your temporary password is: <strong>{temporaryPassword}</strong>. Please change it upon your first login.</p>
                <p>If you have any questions, feel free to reach out to the HR team at {_smtpSettings.SenderEmail}.</p>
                <p>Best regards,<br>{_smtpSettings.SenderName}</p>"
            };
            Console.WriteLine("Email body built");

            string hrDocsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", "hr-manual.pdf");
            string companyPresentationPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "docs", "company-presentation.pdf");

            if (System.IO.File.Exists(hrDocsPath))
            {
                var hrDocInfo = new FileInfo(hrDocsPath);
                Console.WriteLine($"HR manual found at {hrDocsPath}, size: {hrDocInfo.Length} bytes");
                try
                {
                    hrDocStream = new FileStream(hrDocsPath, FileMode.Open, FileAccess.Read);
                    var attachment = new MimePart("application", "pdf")
                    {
                        Content = new MimeContent(hrDocStream),
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        ContentTransferEncoding = ContentEncoding.Base64,
                        FileName = "hr-manual.pdf"
                    };
                    bodyBuilder.Attachments.Add(attachment);
                    Console.WriteLine("HR manual attached");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error attaching HR manual: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"HR manual file not found at path: {hrDocsPath}");
            }

            if (System.IO.File.Exists(companyPresentationPath))
            {
                var presentationInfo = new FileInfo(companyPresentationPath);
                Console.WriteLine($"Company presentation found at {companyPresentationPath}, size: {presentationInfo.Length} bytes");
                try
                {
                    presentationStream = new FileStream(companyPresentationPath, FileMode.Open, FileAccess.Read);
                    var attachment = new MimePart("application", "pdf")
                    {
                        Content = new MimeContent(presentationStream),
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        ContentTransferEncoding = ContentEncoding.Base64,
                        FileName = "company-presentation.pdf"
                    };
                    bodyBuilder.Attachments.Add(attachment);
                    Console.WriteLine("Company presentation attached");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error attaching company presentation: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Company presentation file not found at path: {companyPresentationPath}");
            }

            Console.WriteLine($"Number of attachments added: {bodyBuilder.Attachments.Count}");

            emailMessage.Body = bodyBuilder.ToMessageBody();
            Console.WriteLine("Email body set");

            using var client = new SmtpClient();
            Console.WriteLine($"Connecting to SMTP server: {_smtpSettings.Host}:{_smtpSettings.Port}");
            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, SecureSocketOptions.StartTls);
            Console.WriteLine("Connected to SMTP server");

            Console.WriteLine($"Authenticating with username: {_smtpSettings.Username}");
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            Console.WriteLine("Authenticated with SMTP server");

            await client.SendAsync(emailMessage);
            Console.WriteLine("Email sent successfully");

            await client.DisconnectAsync(true);
            Console.WriteLine("Disconnected from SMTP server");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send welcome email: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
        finally
        {
            // Ferme les streams manuellement après l'envoi
            if (hrDocStream != null)
            {
                hrDocStream.Close();
                hrDocStream.Dispose();
            }
            if (presentationStream != null)
            {
                presentationStream.Close();
                presentationStream.Dispose();
            }
        }
    }

    // DTOs
    public class CreateEmployeDTO
    {
        [Required]
        public string Lastname { get; set; }

        [Required]
        public string Firstname { get; set; }

        [Required]
        [Phone]
        public string Telephone { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string Department { get; set; }

        [Required]
        public string Poste { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string TemporaryPassword { get; set; }
    }

    public class UpdateEmployeDTO
    {
        [Required]
        public string Lastname { get; set; }

        [Required]
        public string Firstname { get; set; }

        [Required]
        [Phone]
        public string Telephone { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string Department { get; set; }

        [Required]
        public string Poste { get; set; }
    }

    // Ajoute EmployeDTO qui est utilisé dans les méthodes
    public class EmployeDTO
    {
        public Guid Id { get; set; }
        public string Lastname { get; set; }
        public string Firstname { get; set; }
        public string Telephone { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Poste { get; set; }
    }
} // Ferme correctement la classe EmployeManagementController