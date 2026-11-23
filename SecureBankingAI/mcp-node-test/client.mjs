
import { Client } from "@modelcontextprotocol/client";
import { StdioClientTransport } from "@modelcontextprotocol/client/stdio";
import { resolve } from "node:path";


const serverDll = resolve(
    process.cwd(),
    "..",
    "SecureBankingAI.Mcp.Banking",
    "bin",
    "Debug",
    "net10.0",
    "SecureBankingAI.Mcp.Banking.dll"
);

// Step 1: Obtain a development JWT from the API.
const tokenResponse = await fetch(
    "http://localhost:5273/api/auth/dev-user-token",
    {
        method: "POST"
    }
);

if (!tokenResponse.ok) {
    throw new Error(
        `Failed to obtain JWT: HTTP ${tokenResponse.status}`
    );
}

const tokenData = await tokenResponse.json();
const accessToken = tokenData.access_token;

if (!accessToken) {
    throw new Error("The API did not return an access token.");
}

console.log("Development JWT obtained successfully.");

// Step 2: Configure the MCP transport with the JWT.
const transport = new StdioClientTransport({
    command: "dotnet",
    args: [serverDll],
    env: {
        ...process.env,
        Mcp__AccessToken: accessToken
    }
});

const client = new Client({
    name: "securebankingai-node-test",
    version: "1.0.0"
});


try {
    console.log("Connecting to SecureBankingAI MCP server...");

    await client.connect(transport);

    console.log("\nConnected successfully!");

    // Discover available MCP tools.
    const { tools } = await client.listTools();

    console.log("\nAvailable MCP tools:");

    for (const tool of tools) {
        console.log(`- ${tool.name}: ${tool.description}`);
    }

    // Invoke the account status tool.
    console.log("\nCalling get_account_status...");

    const result = await client.callTool({
        name: "get_account_status",
        arguments: {}
    });
    console.log("\nAccount status response:");
    console.log(JSON.stringify(result, null, 2));

    if (result.isError) {
        console.error("\nThe MCP tool returned an error.");
        process.exitCode = 1;
    }
    console.log("\nCalling get_recent_transactions...");

const transactions = await client.callTool({
    name: "get_recent_transactions",
    arguments: {
        accountId: "ACC-2002",
        maxResults: 5
    }
});

console.log("\nTransaction response:");
console.log(JSON.stringify(transactions, null, 2));
console.log("\nCalling get_closure_policy...");

const closurePolicy = await client.callTool({
    name: "get_closure_policy",
    arguments: {
        question: "What is the banking account closure policy?"
    }
});

console.log("\nClosure policy response:");
console.log(JSON.stringify(closurePolicy, null, 2));

if (closurePolicy.isError) {
    console.error("\nThe closure policy tool returned an error.");
    process.exitCode = 1;
}

 

    
}
catch (error) {
    console.error("\nMCP test failed:", error);
    process.exitCode = 1;
}
finally {
    await client.close();
    console.log("\nMCP connection closed.");
}
