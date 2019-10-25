import { View } from "./View";

export class DataLifecyclePage extends View
{
    constructor(initialStatus:string)
    {
        super();
        this.setStatus(initialStatus);
    }

    protected dataLoading():void
    {
        this.setStatus("loading");
    }

    protected dataAvailable(nonEmpty:boolean):void
    {
        if (nonEmpty)
        {
            this.setStatus("available");
        }
        else
        {
            this.setStatus("none");
        }
    }

    private setStatus(status:string):void
    {
        for (const node of document.querySelectorAll("[data-presence]"))
        {
            if (node instanceof HTMLElement)
            {
                if (node.getAttribute("data-presence") === status)
                {
                    node.style.display = "block";
                }
                else
                {
                    node.style.display = "none";
                }
            }
        }
    }
}