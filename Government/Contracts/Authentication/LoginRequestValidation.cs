﻿using FluentValidation;
using System.Data;

namespace Government.Contracts.Authentication
{
    public class LoginRequestValidation : AbstractValidator<LoginRequest>
    {

        public LoginRequestValidation()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty();
        }
    }
}
