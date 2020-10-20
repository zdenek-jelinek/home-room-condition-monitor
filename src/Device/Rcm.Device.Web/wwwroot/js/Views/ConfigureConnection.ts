export function initialize()
{
    instance = new ConfigureConnectionPage();
}

let instance: ConfigureConnectionPage;

export class ConfigureConnectionPage
{
    private readonly errorMessage = document.querySelector("#erase-error-message")!;

    constructor()
    {
        const _this = this;

        document
            .querySelector("button#erase-configuration")
            ?.addEventListener("click", function () { _this.eraseConfiguration(); });
    }

    private eraseConfiguration()
    {
        fetch(document.URL, { method: "delete", headers: this.getRequestVerificationHeader() })
            .then((r:Response) => 
            {
                if (!r.ok)
                {
                    r.text().then(console.error);
                    this.errorMessage.textContent = "Could not delete configuration";
                }
                else
                {
                    this.errorMessage.textContent = null;
                    window.location.reload();
                }
            })
            .catch((e:any) =>
            {
                console.error(e);
                this.errorMessage.textContent = "Could not delete configuration";
            });
    }

    private getRequestVerificationHeader(): Record<string, string>
    {
        const requestVerificationTokenInput = document
            .querySelector("input[name='__RequestVerificationToken']") as HTMLInputElement;

        return { ["RequestVerificationToken"]: requestVerificationTokenInput.value };
    }
}