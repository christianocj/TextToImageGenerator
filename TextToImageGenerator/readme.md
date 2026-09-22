# TextToImageGenerator

Web app em **Blazor Server (.NET 10)** para gerar **texto em streaming** ou **imagens** a partir de um prompt, usando os modelos de IA da **Cloudflare Workers AI**.

O usuário escolhe o modo (Texto ou Imagem), digita um prompt e, no caso de imagem, define quantas imagens deseja gerar (1 a 10). O texto aparece token a token em tempo real na tela; as imagens são exibidas conforme ficam prontas.

---

## Tecnologias utilizadas

| Tecnologia | Uso no projeto |
|---|---|
| **.NET 10** | Runtime e SDK do projeto |
| **ASP.NET Core Blazor (Server, Interactive Render Mode)** | Interface web interativa sem JavaScript customizado |
| **Cloudflare Workers AI** | Provedor de IA — geração de imagem (FLUX.2 [klein] 4B) e texto (Llama 3.1 8B Instruct) |
| **HttpClientFactory (`AddHttpClient`)** | Cliente HTTP tipado e gerenciado para chamadas à API da Cloudflare |
| **Server-Sent Events (SSE) + `IAsyncEnumerable`** | Streaming de texto token a token, do backend até a UI |
| **`IOptions<T>` (Options Pattern)** | Configuração tipada (`CloudflareAiOptions`) |
| **.NET User Secrets** | Armazenamento seguro de credenciais em ambiente de desenvolvimento |

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download) instalado
- Conta gratuita na [Cloudflare](https://dash.cloudflare.com/sign-up) (não exige cartão de crédito)
- Git instalado (para clonar o repositório)

---

## Clonar o repositório

```bash
git clone https://github.com/<seu-usuario>/TextToImageGenerator.git
cd TextToImageGenerator
```

---

## Obtendo as credenciais da Cloudflare

1. Acesse [dash.cloudflare.com](https://dash.cloudflare.com) e faça login (ou crie uma conta gratuita).
2. Na tela inicial do painel, copie o **Account ID** (aparece no canto direito, ou em *Workers & Pages → Overview*).
3. Vá em **My Profile → API Tokens → Create Token**.
4. Use o template **"Workers AI"** (ou crie um token customizado com permissão **"Workers AI: Read/Edit"**).
5. Copie o token gerado — ele só aparece uma vez.

> O tier gratuito da Cloudflare Workers AI oferece 10.000 "neurons"/dia, suficiente para gerar centenas de imagens e milhares de respostas de texto sem custo.

---

## Configurando as credenciais (User Secrets)

**As credenciais nunca devem ir para o `appsettings.json` nem ser commitadas no Git.** Este projeto usa o [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) do .NET para isso.

Dentro da pasta do projeto, rode:

```bash
dotnet user-secrets init
dotnet user-secrets set "CloudflareAi:AccountId" "seu_account_id_aqui"
dotnet user-secrets set "CloudflareAi:ApiToken" "seu_token_aqui"
```

Isso grava os valores em um arquivo fora da pasta do projeto (em `%APPDATA%\Microsoft\UserSecrets\` no Windows), então nunca corre o risco de ir parar no controle de versão.

### Alternativa: variáveis de ambiente

Se preferir não usar User Secrets (por exemplo, para rodar fora do Visual Studio ou em outro ambiente):

```powershell
setx CloudflareAi__AccountId "seu_account_id_aqui"
setx CloudflareAi__ApiToken "seu_token_aqui"
```

> Repare no duplo underscore (`__`) — é assim que o ASP.NET Core mapeia variáveis de ambiente para seções aninhadas da configuração (`CloudflareAi:AccountId`).

Após definir, **feche e abra o terminal (ou o Visual Studio) novamente** — variáveis de ambiente só são lidas quando o processo inicia.

---

## ▶️ Executando o projeto

```bash
dotnet restore
dotnet run
```

Acesse a URL exibida no terminal (geralmente `https://localhost:5001` ou similar) em um navegador.

---


---

## Como funciona por baixo dos panos

- **Modo Texto**: o `CloudflareAiService.StreamTextAsync` faz uma requisição `POST` com `stream: true` para o modelo de linguagem da Cloudflare, lê a resposta como um stream de eventos SSE (`data: {...}`) linha a linha, e usa `IAsyncEnumerable<string>` para entregar cada pedaço de texto ao componente Blazor assim que chega — sem esperar a resposta completa.
- **Modo Imagem**: o `CloudflareAiService.GenerateImageAsync` envia o prompt como `multipart/form-data` para o modelo FLUX.2 [klein] 4B, recebe a imagem em base64 dentro do JSON de resposta, e a página converte isso em uma `data:image/png;base64,...` para exibir direto no `<img>`, sem precisar salvar arquivo em disco.

---

## Segurança

- Nunca cole tokens/API keys diretamente no código-fonte ou em mensagens de chat/commits.
- Se qualquer credencial deste projeto for exposta acidentalmente, revogue-a imediatamente em [dash.cloudflare.com/profile/api-tokens](https://dash.cloudflare.com/profile/api-tokens) e gere uma nova.
- O `.gitignore` já exclui `appsettings.Development.json` e pastas de build (`bin/`, `obj/`), mas revise antes de cada commit se não há segredos hardcoded.