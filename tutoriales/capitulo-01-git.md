# Capítulo 1 — Git

## Objetivo

Entender Git usando este proyecto real, no copiando comandos de memoria.

Git no es GitHub. Git es el sistema de control de versiones local; GitHub es un servidor donde podemos alojar una copia remota del repositorio.

## Estado inicial del proyecto

El proyecto ya tenía archivos, pero la carpeta todavía no era un repositorio Git local.

Para comprobarlo usamos:

```bash
git status --short
git remote -v
```

El error fue:

```txt
fatal: not a git repository
```

Eso significa que no existía la carpeta oculta `.git/` en el proyecto ni en sus carpetas padre.

## Inicializar Git

```bash
git init
```

Este comando crea `.git/`, que es donde Git guarda su base de datos local: commits, ramas, referencias, configuración y estado del repositorio.

Después de esto, la carpeta ya es un repositorio Git local.

## Conectar el repositorio local con GitHub

```bash
git remote add origin https://github.com/emicortez/LearnHub.git
```

Un `remote` es una referencia a otro repositorio, normalmente uno alojado en GitHub.

`origin` es el nombre convencional del repositorio remoto principal.

Para verificarlo:

```bash
git remote -v
```

Deberíamos ver algo como:

```txt
origin  https://github.com/emicortez/LearnHub.git (fetch)
origin  https://github.com/emicortez/LearnHub.git (push)
```

## Ver archivos no trackeados

```bash
git status --short
```

Si aparece `??` delante de un archivo o carpeta, significa que Git ve ese archivo pero todavía no lo está siguiendo.

Ejemplo:

```txt
?? docs/
?? src/
?? README.md
```

Todavía no hay commits locales; solo tenemos archivos sueltos dentro de un repo inicializado.

## Ver si el repositorio remoto ya tiene ramas

```bash
git ls-remote --heads origin
```

Este comando consulta las ramas existentes en GitHub sin modificar nuestros archivos locales.

En este proyecto vimos que existe:

```txt
refs/heads/main
```

Eso significa que GitHub ya tiene una rama `main`. Por eso NO conviene hacer commit y push inmediatamente sin revisar, porque podríamos pisar o chocar con historia remota.

## Próximos comandos seguros

Estos comandos sirven para inspeccionar el remoto sin mezclar cambios todavía.

### Traer referencias remotas

```bash
git fetch origin
```

`fetch` descarga información del remoto, como `origin/main`, pero no modifica nuestros archivos de trabajo.

### Ver últimos commits del remoto

```bash
git log --oneline origin/main -5
```

Muestra los últimos 5 commits de la rama remota `main`.

### Ver archivos que existen en el remoto

```bash
git ls-tree -r --name-only origin/main
```

Lista los archivos versionados en `origin/main`.

### Revisar estado local

```bash
git status --short
```

Muestra qué archivos locales están sin trackear, modificados o listos para commit.

## Regla de aprendizaje

Antes de ejecutar comandos que cambian historia o mezclan código, primero inspeccionamos:

1. Qué tengo localmente.
2. Qué existe en remoto.
3. Si hay riesgo de pisar trabajo.

Git se aprende entendiendo estados, no memorizando comandos.

## Resultado real al inspeccionar el remoto

Ejecutamos:

```bash
git fetch origin
```

Resultado relevante:

```txt
From https://github.com/emicortez/LearnHub
 * [new branch]      main       -> origin/main
```

Esto significa que Git descargó la referencia remota `origin/main`. Todavía no mezcló nada con nuestros archivos locales.

Después ejecutamos:

```bash
git log --oneline origin/main -5
```

Resultado:

```txt
3a86b82 (origin/main) Initial commit
```

El remoto tiene un solo commit llamado `Initial commit`.

Después revisamos qué archivos contiene ese commit remoto:

```bash
git ls-tree -r --name-only origin/main
```

Resultado:

```txt
.gitignore
```

Conclusión: GitHub solo tiene `.gitignore`. Nuestro proyecto local tiene casi todo el contenido real del proyecto.

Finalmente revisamos el estado local:

```bash
git status --short
```

Resultado:

```txt
?? .atl/
?? .claude/
?? CLAUDE.md
?? JobOffers/
?? LearnHub.slnx
?? NuGet.config
?? README.md
?? docs/
?? openspec/
?? src/
?? tutoriales/
```

`??` significa que esos archivos existen localmente, pero todavía no están versionados por Git.

## Decisión segura

Como el remoto solo tiene `.gitignore`, el siguiente paso es integrar ese commit remoto antes de crear nuestro primer commit local grande.

La forma segura es crear una rama local `main` basada en `origin/main`:

```bash
git switch -c main origin/main
```

Esto conecta nuestra rama local `main` con la rama remota `origin/main`.

Después volvemos a revisar estado:

```bash
git status --short
```

Los archivos del proyecto deberían seguir apareciendo como `??`, porque todavía no los agregamos al índice.

## Crear la rama local basada en el remoto

Ejecutamos:

```bash
git switch -c main origin/main
```

Resultado:

```txt
branch 'main' set up to track 'origin/main'.
Switched to a new branch 'main'
```

Esto significa que ahora tenemos una rama local `main` conectada a `origin/main`.

La palabra `track` indica que Git sabe que nuestra rama local `main` corresponde a la rama remota `origin/main`. Gracias a eso, comandos como `git pull` y `git push` pueden inferir el destino por defecto.

Después revisamos:

```bash
git status --short
```

Resultado:

```txt
?? .atl/
?? .claude/
?? CLAUDE.md
?? JobOffers/
?? LearnHub.slnx
?? NuGet.config
?? README.md
?? docs/
?? openspec/
?? src/
?? tutoriales/
```

Esto es correcto: ya estamos parados sobre `main`, pero todavía no agregamos los archivos locales al índice de Git.

## Revisar antes de agregar

Antes de usar `git add`, conviene revisar `.gitignore` y decidir qué archivos deberían versionarse.

El `.gitignore` remoto ya ignora carpetas típicas de .NET como `bin/`, `obj/`, `Debug/`, `Release/`, logs, paquetes NuGet generados y `.env`.

Eso es bueno: evita subir resultados de compilación, archivos temporales o secretos de entorno.

## Estado completo después de conectar `main`

Ejecutamos:

```bash
git status
```

Resultado:

```txt
On branch main
Your branch is up to date with 'origin/main'.

Untracked files:
  (use "git add <file>..." to include in what will be committed)
        .atl/
        .claude/
        CLAUDE.md
        JobOffers/
        LearnHub.slnx
        NuGet.config
        README.md
        docs/
        openspec/
        src/
        tutoriales/

nothing added to commit but untracked files present (use "git add" to track)
```

Interpretación:

- `On branch main`: estamos trabajando en la rama local `main`.
- `Your branch is up to date with 'origin/main'`: nuestra rama local y la remota apuntan al mismo commit.
- `Untracked files`: Git ve esos archivos, pero todavía no los incluyó en ningún commit.
- `nothing added to commit`: el área de staging está vacía.

El próximo paso no es agregar todo automáticamente. Primero hay que decidir qué carpetas pertenecen al repositorio y cuáles son locales/personales.

## No subir material personal

Detectamos que `JobOffers/` contiene archivos `.docx` con ofertas laborales.

Aunque pueden servir como contexto personal de estudio, no conviene subirlos a un repositorio público porque pueden contener información sensible: empresas, contactos, condiciones, links privados o datos personales.

Por eso agregamos esta regla a `.gitignore`:

```gitignore
JobOffers/
```

Esto le indica a Git que ignore esa carpeta completa.

## Warnings de LF y CRLF en Windows

Al ejecutar:

```bash
git add .
```

Git mostró varios warnings como:

```txt
warning: in the working copy of 'README.md', LF will be replaced by CRLF the next time Git touches it
```

Esto no es un error. Es una advertencia sobre finales de línea.

- `LF` es el salto de línea típico de Linux/macOS.
- `CRLF` es el salto de línea típico de Windows.

El problema no es visual: el problema aparece cuando un equipo mezcla sistemas operativos y Git empieza a marcar archivos como modificados solo por cambios de saltos de línea.

Para evitar ruido en los diffs, agregamos un archivo `.gitattributes`.

`.gitattributes` define reglas de versionado para el repositorio. En este caso, lo usamos para normalizar archivos de código, documentación y configuración con `LF`.

Ejemplo:

```gitattributes
*.cs text eol=lf
*.md text eol=lf
*.json text eol=lf
*.docx binary
```

La regla `*.docx binary` le dice a Git que los documentos de Office son binarios y no debe intentar normalizarles saltos de línea.

Después de agregar `.gitattributes`, hay que stagearlo también:

```bash
git add .gitattributes .gitignore tutoriales/capitulo-01-git.md
git status --short
```
