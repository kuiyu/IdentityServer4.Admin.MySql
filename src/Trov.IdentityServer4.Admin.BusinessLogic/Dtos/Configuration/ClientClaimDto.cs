﻿using System.ComponentModel.DataAnnotations;

namespace Trov.IdentityServer4.Admin.BusinessLogic.Dtos.Configuration
{
	public class ClientClaimDto
	{
	    public int Id { get; set; }

	    [Required]
        public string Type { get; set; }

	    [Required]
        public string Value { get; set; }
	}
}