class TwoFactorMethod extends Enum
{
    static TwoFactorByEmail = 0;
    static TwoFactorByPhoneNumber = 1;
}
class ContactType extends Enum
{
    static EMail = 0;
    static PhoneNumber = 1;
}
var elemForm = null;
function validateInputElement(element, errorMessages)
{
	if(element instanceof HTMLInputElement)
	{
		const name = element.getAttribute("name");
		var validationMessage = "";
		if((!element.checkValidity() || element.getAttribute("required") === "true" && ObjectExtensions.isEmptyString(element.value))
			&& ObjectExtensions.isNotEmptyString(errorMessages[name]))
		{
			validationMessage = errorMessages[name];
		}
		else if(name === "PasswordConfirmation" && element.value?.trim() !== document.querySelector("#Password").value?.trim())
		{
			validationMessage = "Password confirmation don't match password";
		}
		element.setCustomValidity(validationMessage);
	}
}
function getModelFromForm()
{
    let model = {};
    for(const element of document.querySelectorAll("input.account-property-input"))
    {
        if(element instanceof HTMLInputElement && ObjectExtensions.isNotEmptyString(element.name))
        {
            model[element.name] = element.value;
        }
    }
    let checkbox = document.querySelector("input#enable-two-factor-checkbox");
    if(checkbox instanceof HTMLInputElement)
    {
        model[checkbox.name] = checkbox.checked;
    }
    if(checkbox.checked)
    {
        radio = document.querySelector("input#two-factor-by-phone-radio");
        model.TwoFactorMethod = TwoFactorMethod.getValueByKey(radio.checked ? "TwoFactorByPhoneNumber" : "TwoFactorByEmail");
    }
    return model;
}
async function confirmContact1(event)
{
    if(event.target instanceof HTMLInputElement)
    {
        let model = getModelFromForm();
        if(Object.keys(model)?.length > 0)
        {
            const urlAction = window.location.origin + "/UserAccount/ConfirmContact";
            const jqXHR = $.ajax({
                method: "POST",
                url: urlAction,
                data: { model: model }
            });
            await jqXHR?.then(
                async function(data, textStatus, jqxhr)
                {
                    alert(`request to ${urlAction} is successed`);
                },
                function(jqxhr, textStatus, errorThrown)
                {
                    alert(`request to ${urlAction} is failed; status: ${textStatus}; error: ${errorThrown}`);
                });

        }
    }
}
async function confirmContact(event)
{
    if(event.target instanceof HTMLInputElement)
    {
        let formData = new FormData(elemForm);
        formData.set("TwoFactorEnabled", formData.get("TwoFactorEnabled") === "on" ? "true" : "false");
        if([...formData?.keys()]?.length > 0)
        {
            const urlAction = window.location.origin + "/UserAccount/ConfirmContact";
            const jqXHR = $.ajax({
                method: "POST",
                url: urlAction,
                data: formData,
                processData: false,
                contentType: false
            });
            await jqXHR?.then(
                async function(data, textStatus, jqxhr)
                {
                    alert(`request to ${urlAction} is successed`);
                },
                function(jqxhr, textStatus, errorThrown)
                {
                    alert(`request to ${urlAction} is failed; status: ${textStatus}; error: ${errorThrown}`);
                });

        }
    }
}
window.addEventListener("load", async function() 
{
    elemForm = document.querySelector("form.account-form");
    let displayNames = {};
    let errorDescriptions = {};
    if(elemForm instanceof HTMLFormElement)
    {
        jsonModel = elemForm.dataset["model"];
        let jsonDisplayNames = elemForm.dataset["displayNames"];
        let jsonErrorDescriptions = elemForm.dataset["errorDescriptions"];
        displayNames = JSON.parse(jsonDisplayNames);
        errorDescriptions = JSON.parse(jsonErrorDescriptions);
    }
    let nodeList = document.querySelectorAll("input.account-property-input");
    for(curElem of nodeList)
    {
        if(curElem instanceof HTMLInputElement)
        {
            curElem.addEventListener("change", function(event) 
            {
                validateInputElement(event.target, errorDescriptions);
            });
            validateInputElement(curElem, errorDescriptions);
        }
    }
    let elemCheckbox = document.querySelector("#enable-two-factor-checkbox");
    if(elemCheckbox instanceof HTMLInputElement)
    {
        elemCheckbox.addEventListener("change", function(event) 
        {
            if(event.target instanceof HTMLInputElement)
            {
                let elemFieldset = document.querySelector("fieldset.two-factor-method-fieldset");
                if(elemFieldset instanceof HTMLFieldSetElement)
                {
                    elemFieldset.disabled = !event.target.checked;
                }
            }
        });
        let event = new Event("change");
        elemCheckbox.dispatchEvent(event);
    }
    let elemConfirmEmailButton = document.querySelector("#confirm-email-button");
    let elemConfirmPhoneButton = document.querySelector("#confirm-phone-button");
    if(elemConfirmEmailButton instanceof HTMLInputElement)
    {
        elemConfirmEmailButton.addEventListener("click", confirmContact);
    }
    if(elemConfirmPhoneButton instanceof HTMLInputElement)
    {
        elemConfirmPhoneButton.addEventListener("click", confirmContact);
    }
    let elemCloseButton = document.querySelector("#close-button");
    if(elemCloseButton instanceof HTMLInputElement)
    {
        elemCloseButton.addEventListener("click", function(event) 
        {
            location.href = window.location.origin + "/Home/Start"
        });
    }
});