export class View
{
    protected setElementText(parent:ParentNode, selector:string, text:string):void
    {
        const element = parent.querySelector(selector);
        if (!element)
        {
            throw new Error("Could not set value: No element with selector " + selector + " found");
        }

        element.textContent = text;
    }
}