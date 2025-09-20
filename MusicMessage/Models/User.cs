using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicMessage.Models;

public partial class User
{
	public int UserId { get; set; }

	[Required]
	[MaxLength(50)]
	public string UserName { get; set; } = null!;
	public User Clone()
	{
		return new User
		{
			UserId = this.UserId,
			UserName = this.UserName,
			Email = this.Email,
			FirstName = this.FirstName,
			LastName = this.LastName,
			DateOfBirth = this.DateOfBirth,
			Gender = this.Gender,
			City = this.City,
			Country = this.Country,
			Bio = this.Bio,
			AvatarPath = this.AvatarPath,
			ProfileCoverPath = this.ProfileCoverPath,
			IsOnline = this.IsOnline,
			LastSeen = this.LastSeen
		};
	}
	[Required]
	[MaxLength(100)]
	public string Email { get; set; } = null!;
	[MaxLength(100)]
	public string FirstName { get; set; }
	[MaxLength(100)]
	public string LastName { get; set; }
	public DateTime? DateOfBirth { get; set; }
	[MaxLength(10)]
	public string? Gender { get; set; }
	[MaxLength(100)]
	public string? City { get; set; }
	[MaxLength(100)]
	public string? Country { get; set; }

	[MaxLength(500)]
	public string? Bio { get; set; }
	public string? ProfileCoverPath { get; set; }
	[NotMapped]
	public string FullName => $"{FirstName} {LastName}".Trim();

	[NotMapped]
	public int? Age
	{
		get
		{
			if (DateOfBirth == null) return null;

			var today = DateTime.Today;
			var birthDate = DateOfBirth.Value;
			var age = today.Year - birthDate.Year;

			// Проверяем, был ли уже день рождения в этом году
			if (birthDate > today.AddYears(-age))
				age--;

			return age;
		}
	}

	[NotMapped]
	public string Location
	{
		get
		{
			if (string.IsNullOrEmpty(City) && string.IsNullOrEmpty(Country))
				return "Местоположение не указано";

			if (!string.IsNullOrEmpty(City) && !string.IsNullOrEmpty(Country))
				return $"{City}, {Country}";

			return City ?? Country;
		}
	}

	[NotMapped]
	public string Status
	{
		get
		{
			if (IsOnline) return "Онлайн";
			if (LastSeen.HasValue) return $"Был(а) в {LastSeen.Value:HH:mm}";
			return "Оффлайн";
		}
	}
	[Required]
	public string PasswordHash { get; set; } = null!;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public DateTime? LastLogin { get; set; }
	public DateTime? LastSeen { get; set; }
	public string? AvatarPath { get; set; }
	public bool IsOnline { get; set; }

	public virtual ICollection<Message> MessageReceivers { get; set; } = new List<Message>();

	public virtual ICollection<Message> MessageSenders { get; set; } = new List<Message>();
}
