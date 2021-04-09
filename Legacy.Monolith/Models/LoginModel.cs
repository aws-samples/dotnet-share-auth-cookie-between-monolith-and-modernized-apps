using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Legacy.Monolith
{

  /* FYI:
	 * 1) Metadata annotation attributes will help some of MVC’s HTML helpers to build the login form.
	 * 2) ReturnUrl property is decorated with the HiddenInput and ScaffoldColumn(false) attributes.
	 * HiddenInput attribute indicates that this property would be rendered as a hidden input element.	
	 * Also, the ScaffoldColumn(false) will tell the razor view not to build the form elements: this property should not be displayed as an input element.
	 */
  public class LoginModel
  {
		[Required]
		[DataType(DataType.EmailAddress)]
		public string Email { get; set; }

		[Required]
		[DataType(DataType.Password)]
		public string Password { get; set; }

		[HiddenInput]
		[ScaffoldColumn(false)]
		public string ReturnUrl { get; set; }
	}
}