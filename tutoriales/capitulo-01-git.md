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

El warning sobre `.gitattributes` puede aparecer una vez más porque el propio archivo recién fue agregado y Git todavía está aplicando la política de finales de línea.

## Staging area

Después de ejecutar `git add`, muchos archivos aparecen con `A`:

```txt
A  .gitattributes
A  CLAUDE.md
A  LearnHub.slnx
A  README.md
A  docs/architecture.md
A  src/Services/Identity/Identity.Domain/Entities/User.cs
A  tutoriales/capitulo-01-git.md
```

`A` significa `Added`: el archivo está en el área de staging y va a entrar en el próximo commit.

También aparece:

```txt
M  .gitignore
```

`M` significa `Modified`: `.gitignore` ya existía en el commit remoto, pero ahora tiene cambios locales agregados al staging.

En este punto todavía no existe un commit nuevo. Solo preparamos el contenido que va a formar parte del commit.

Antes de commitear, conviene revisar el resumen de lo staged:

```bash
git diff --cached --stat
```

Y si queremos ver el diff completo:

```bash
git diff --cached
```

Para el primer commit del proyecto, normalmente alcanza con revisar el resumen y algunos archivos clave.

## Modelo mental de Git

Git se entiende mejor si pensamos en cuatro lugares:

```txt
Working tree  ->  Staging area  ->  Local repository  ->  Remote repository
archivos          git add            git commit            git push
```

### Working tree

Es la carpeta real donde editamos archivos.

Ejemplo: cuando modificamos `tutoriales/capitulo-01-git.md`, ese cambio está en el working tree.

### Staging area

Es una zona intermedia donde preparamos exactamente qué queremos incluir en el próximo commit.

```bash
git add tutoriales/capitulo-01-git.md
```

Este comando no crea un commit. Solo prepara el archivo.

### Local repository

Es la historia de commits guardada en nuestra máquina dentro de `.git/`.

```bash
git commit -m "docs: update git tutorial notes"
```

Este comando crea una foto versionada de lo que estaba en staging.

### Remote repository

Es la copia alojada en GitHub.

```bash
git push
```

Este comando sube nuestros commits locales al remoto.

## Primer commit del proyecto

Después de revisar el staging, creamos el primer commit grande del proyecto:

```bash
git commit -m "chore: add initial LearnHub project structure"
```

Partes del mensaje:

- `chore`: tipo de cambio. Se usa para tareas de mantenimiento, configuración o estructura inicial.
- `add initial LearnHub project structure`: descripción corta en imperativo.

El commit quedó así:

```txt
d62ff49 chore: add initial LearnHub project structure
```

Un commit es una foto del estado preparado en staging. No sube automáticamente a GitHub.

## Subir el commit a GitHub

Después del commit, subimos a la rama remota:

```bash
git push
```

Como nuestra rama local `main` ya estaba trackeando `origin/main`, no hizo falta escribir:

```bash
git push origin main
```

Ambos apuntan al mismo destino en este caso.

Verificamos con:

```bash
git log --oneline --decorate -3
```

Resultado:

```txt
d62ff49 (HEAD -> main, origin/main) chore: add initial LearnHub project structure
3a86b82 Initial commit
```

Interpretación:

- `HEAD -> main`: nuestro repositorio local está parado en `main`.
- `origin/main`: el remoto también apunta al mismo commit.
- Si `HEAD -> main` y `origin/main` están en el mismo commit, local y remoto están sincronizados.

## Cambios locales después del push

Después del push, revisamos:

```bash
git status --short
```

Y vimos:

```txt
M tutoriales/capitulo-01-git.md
```

Eso significa que el repositorio remoto está actualizado, pero el archivo del tutorial tiene cambios locales nuevos que todavía no fueron commiteados.

Esto está bien si el archivo sigue en progreso.

Cuando queramos guardar ese avance:

```bash
git add tutoriales/capitulo-01-git.md
git commit -m "docs: update git tutorial notes"
git push
```

## Códigos comunes de `git status --short`

```txt
?? archivo   # Untracked: Git todavía no lo sigue
A  archivo   # Added: archivo nuevo preparado para commit
M  archivo   # Modified: archivo modificado
AM archivo   # Added en staging, pero modificado otra vez después
D  archivo   # Deleted: archivo borrado
```

Detalles importantes:

- La columna izquierda representa el staging area.
- La columna derecha representa el working tree.

Por ejemplo:

```txt
AM tutoriales/capitulo-01-git.md
```

Significa que el archivo fue agregado al staging, pero después volvió a cambiar en el working tree. Para incluir la versión más nueva:

```bash
git add tutoriales/capitulo-01-git.md
```

## Comandos de inspección que conviene usar mucho

### Ver estado resumido

```bash
git status --short
```

Bueno para trabajar rápido.

### Ver estado explicado

```bash
git status
```

Bueno para aprender o confirmar qué está pasando.

### Ver últimos commits

```bash
git log --oneline --decorate -5
```

Muestra los últimos commits y dónde apuntan `HEAD`, ramas locales y ramas remotas.

### Ver cambios no stageados

```bash
git diff
```

Muestra cambios en el working tree que todavía no están en staging.

### Ver cambios stageados

```bash
git diff --cached
```

Muestra lo que entraría en el próximo commit.

### Ver resumen de cambios stageados

```bash
git diff --cached --stat
```

Muestra archivos y cantidad aproximada de líneas agregadas o modificadas.

## Comandos para corregir antes de commitear

### Sacar un archivo del staging sin borrar cambios

```bash
git restore --staged archivo
```

Esto deshace el `git add` para ese archivo, pero conserva los cambios en el working tree.

### Descartar cambios locales de un archivo

```bash
git restore archivo
```

Cuidado: esto borra los cambios locales no commiteados de ese archivo.

### Ver qué se va a commitear

```bash
git diff --cached
```

Regla práctica: antes de `git commit`, deberíamos poder explicar qué estamos commiteando y por qué.

## `fetch`, `pull` y `push`

### `git fetch`

```bash
git fetch origin
```

Trae información del remoto, pero no mezcla cambios en nuestros archivos.

Es el comando seguro para inspeccionar.

### `git pull`

```bash
git pull
```

Trae cambios del remoto y los integra en nuestra rama actual.

Conceptualmente suele ser parecido a:

```bash
git fetch
git merge
```

No lo usamos a ciegas cuando no sabemos qué cambió en remoto.

### `git push`

```bash
git push
```

Sube nuestros commits locales al remoto configurado.

Si la rama no está trackeando un remoto, puede hacer falta:

```bash
git push -u origin nombre-rama
```

`-u` configura el tracking entre la rama local y la remota.

## Branching para trabajar como dev profesional

Aunque este proyecto empezó trabajando directo sobre `main`, en un equipo profesional normalmente no se desarrolla directo en `main`.

La regla sana es:

```txt
main = código estable
feature branch = trabajo en progreso
pull request = revisión antes de mezclar
```

### Crear una rama de trabajo

```bash
git switch -c feat/identity-register-user
```

`switch -c` crea una rama nueva y se mueve a ella.

Convención recomendada:

```txt
feat/nombre-corto
fix/nombre-corto
docs/nombre-corto
chore/nombre-corto
refactor/nombre-corto
test/nombre-corto
```

Ejemplos:

```bash
git switch -c feat/identity-register-user
git switch -c test/identity-domain
git switch -c docs/git-tutorial
```

### Ver ramas

```bash
git branch
```

Muestra ramas locales.

```bash
git branch -vv
```

Muestra ramas locales, último commit y si trackean una rama remota.

### Cambiar de rama

```bash
git switch main
git switch feat/identity-register-user
```

Antes de cambiar de rama, conviene revisar:

```bash
git status --short
```

Si hay cambios locales sin commitear, Git puede bloquear el cambio o arrastrar esos cambios a la otra rama.

## Commits profesionales

Un commit profesional no es “guardé cosas”. Es una unidad de cambio entendible.

Un buen commit debería responder:

1. Qué cambió.
2. Por qué cambió.
3. Qué parte del sistema toca.
4. Si se puede revisar sin mezclar diez ideas distintas.

### Tamaño recomendado

Mal commit:

```txt
feat: update project
```

Incluye API, DB, tests, docs, refactors y cambios de formato. Eso es imposible de revisar bien.

Mejor:

```txt
test: add identity domain unit tests
feat: add register user use case
docs: document identity registration flow
```

Cada commit tiene un propósito.

### Mensajes estilo Conventional Commits

Usaremos este formato:

```txt
tipo: descripcion corta
```

Tipos comunes:

| Tipo | Uso |
|------|-----|
| `feat` | Nueva funcionalidad |
| `fix` | Corrección de bug |
| `docs` | Documentación |
| `test` | Tests |
| `refactor` | Cambio interno sin modificar comportamiento |
| `chore` | Mantenimiento/configuración |
| `build` | Build system, NuGet, tooling |
| `ci` | Pipelines/automatización |

Ejemplos:

```bash
git commit -m "test: add email value object tests"
git commit -m "feat: add user registration command"
git commit -m "docs: explain git staging area"
```

### Commits atómicos

Un commit atómico contiene una sola idea.

Ejemplo en TDD:

```txt
test: add failing email validation tests
feat: enforce email normalization rules
refactor: simplify email factory validation
```

Esto permite revisar, revertir y entender mejor la historia.

## Pull Request mental model

Un Pull Request no es solo “subir código”. Es una propuesta de cambio.

Debería explicar:

- qué problema resuelve,
- qué cambió,
- cómo se probó,
- qué queda fuera de alcance,
- qué riesgos tiene.

Flujo típico:

```bash
git switch main
git pull
git switch -c feat/identity-register-user
# trabajar
git add ...
git commit -m "feat: add register user use case"
git push -u origin feat/identity-register-user
```

Después se abre un PR desde `feat/identity-register-user` hacia `main`.

## Merge, rebase y squash

Estas son formas de integrar cambios.

### Merge

```bash
git merge nombre-rama
```

Crea una integración preservando la historia de ambas ramas.

Ventaja: no reescribe historia.

Desventaja: puede crear más commits de merge.

### Rebase

```bash
git rebase main
```

Reaplica tus commits encima de otra base.

Ventaja: historia más lineal.

Riesgo: reescribe historia. No conviene hacer rebase de commits que otros ya están usando.

Regla senior:

```txt
Rebase en tu rama local: sí.
Rebase en ramas compartidas: cuidado extremo.
```

### Squash

Squash combina varios commits en uno.

Es útil cuando durante el desarrollo hicimos commits pequeños o desordenados y queremos que `main` reciba una historia más limpia.

En GitHub suele aparecer como:

```txt
Squash and merge
```

## Resolver conflictos

Un conflicto aparece cuando Git no puede decidir automáticamente cómo combinar cambios.

Ejemplo típico:

```txt
<<<<<<< HEAD
versión actual
=======
versión que viene de otra rama
>>>>>>> feature-branch
```

No hay que entrar en pánico. Un conflicto es Git diciendo:

> “Necesito que un humano decida qué contenido final corresponde”.

Flujo sano:

```bash
git status
# abrir archivos con conflicto
# editar y dejar la versión correcta
git add archivo-resuelto
git status
git commit
```

Regla importante: después de resolver, el archivo no debe contener marcas `<<<<<<<`, `=======` ni `>>>>>>>`.

Para buscarlas:

```bash
git diff --check
```

## Stash: guardar cambios temporalmente

`stash` sirve para guardar cambios sin commitearlos.

Ejemplo:

```bash
git stash push -m "wip git tutorial"
```

Ver stashes:

```bash
git stash list
```

Recuperar el último stash:

```bash
git stash pop
```

Útil cuando necesitás cambiar de rama pero tenés trabajo local incompleto.

No hay que abusar: si usás stash como basurero permanente, después no sabés qué contiene cada cosa.

## `.gitignore` y `.gitattributes`

### `.gitignore`

Define qué archivos Git debería ignorar.

En .NET normalmente se ignora:

```gitignore
bin/
obj/
.vs/
.env
*.user
```

En este proyecto agregamos:

```gitignore
JobOffers/
```

Porque no pertenece al producto y puede contener información personal.

### `.gitattributes`

Define cómo Git debe tratar ciertos archivos.

Lo usamos para normalizar finales de línea y evitar ruido entre Windows/Linux/macOS.

Ejemplo:

```gitattributes
*.cs text eol=lf
*.md text eol=lf
*.docx binary
```

Para un proyecto .NET senior, `.gitignore` y `.gitattributes` no son detalles menores: evitan commits basura, diffs falsos y problemas entre sistemas operativos.

## Git en proyectos .NET

Un dev .NET senior debe saber qué archivos se versionan y cuáles no.

### Sí suelen versionarse

```txt
*.sln / *.slnx
*.csproj
Directory.Build.props
Directory.Packages.props
NuGet.config
global.json
src/
tests/
docs/
README.md
.editorconfig
.gitignore
.gitattributes
```

### No suelen versionarse

```txt
bin/
obj/
.vs/
TestResults/
*.user
*.suo
.env
coverage/
*.nupkg
```

### `NuGet.config`

En este proyecto `NuGet.config` sí debe versionarse porque controla las fuentes NuGet y evita problemas con feeds globales que requieren credenciales.

Regla: no subir credenciales en `NuGet.config`. Si un feed necesita credenciales, se gestionan fuera del repo o con secretos del entorno/CI.

## Seguridad: secretos e historia Git

Nunca subir secretos. Pero además hay que entender algo más serio:

> Borrar un secreto del archivo no lo borra de la historia Git.

Si commiteás un token y después lo eliminás, el token sigue existiendo en commits anteriores.

Qué hacer si pasa:

1. Revocar el secreto inmediatamente.
2. Rotar credenciales.
3. Limpiar historia solo si hace falta y sabiendo el impacto.
4. Avisar al equipo.

Comandos útiles para inspeccionar antes de commitear:

```bash
git diff --cached
git diff --check
```

`git diff --check` detecta problemas como espacios finales y marcas de conflicto.

## Tags y versiones

Un tag marca un punto importante de la historia.

Ejemplo:

```bash
git tag v0.1.0
git push origin v0.1.0
```

En productos reales se usan para releases:

```txt
v1.0.0
v1.1.0
v2.0.0
```

No necesitamos tags todavía, pero conviene saber que existen.

## Comandos de recuperación básicos

### Ver qué cambió en un archivo

```bash
git diff archivo
```

### Descartar cambios no deseados

```bash
git restore archivo
```

### Sacar del staging

```bash
git restore --staged archivo
```

### Corregir el último commit local

```bash
git commit --amend
```

Usar `--amend` solo antes de pushear o cuando sabés exactamente qué estás haciendo. Cambia el commit anterior.

### Revertir un commit ya publicado

```bash
git revert <commit-sha>
```

`revert` crea un nuevo commit que deshace otro commit. Es más seguro que reescribir historia cuando el cambio ya fue compartido.

## `reset` vs `revert`

### `git reset`

Mueve una rama hacia otro commit. Puede sacar commits de la historia visible.

Es potente, pero peligroso.

Ejemplo para deshacer staging:

```bash
git reset
```

Ejemplo peligroso:

```bash
git reset --hard HEAD~1
```

`--hard` descarta cambios. No usar por reflejo.

### `git revert`

Crea un commit nuevo que deshace uno anterior.

Regla práctica:

```txt
Cambio ya pusheado o compartido -> preferir revert.
Cambio local y no compartido -> reset/restore puede ser aceptable.
```

## Worktrees

`git worktree` permite tener varias carpetas trabajando sobre el mismo repo con ramas distintas.

Ejemplo:

```bash
git worktree add ../LearnHub-tdd feat/strict-tdd
```

Es útil para revisar una rama mientras seguís trabajando en otra sin hacer stash todo el tiempo.

No es necesario para este proyecto ahora, pero es una herramienta senior muy útil.

## Checklist senior antes de commitear

Antes de un commit serio:

```bash
git status --short
git diff
git diff --cached --stat
git diff --cached
git diff --check
```

Checklist mental:

- ¿Estoy en la rama correcta?
- ¿El commit tiene una sola intención?
- ¿No estoy subiendo secretos?
- ¿No estoy subiendo archivos generados?
- ¿El mensaje explica el cambio?
- ¿Corrí build/tests cuando corresponde?
- ¿El diff lo podría revisar otra persona sin odiarme?

En .NET, antes de pushear cambios de código suele corresponder:

```bash
dotnet build LearnHub.slnx
dotnet test LearnHub.slnx
```

Hoy `dotnet test` todavía no aplica bien porque recién vamos a crear infraestructura de tests. Cuando activemos strict TDD, va a pasar a ser obligatorio.

## Flujo recomendado para LearnHub

Para este proyecto vamos a usar este flujo:

1. `main` se mantiene estable.
2. Cada cambio importante va en una rama.
3. Los commits siguen Conventional Commits.
4. No se suben archivos personales ni secretos.
5. Los cambios de código deben ir acompañados por tests cuando strict TDD esté activo.
6. Los tutoriales se commitean como `docs`.

Ejemplo:

```bash
git switch main
git pull
git switch -c test/enable-strict-tdd

# crear tests / ajustar proyecto

git status --short
git diff
git add tests/ LearnHub.slnx
git commit -m "test: add identity domain test project"
git push -u origin test/enable-strict-tdd
```

Esto nos fuerza a trabajar como se trabaja en proyectos reales: cambios chicos, revisables y explicables.

## Qué NO hacer por reflejo

### No usar `git add .` sin mirar

`git add .` es útil, pero puede meter archivos personales, temporales o generados.

Antes conviene revisar:

```bash
git status --short
```

### No commitear secretos

Nunca commitear:

- `.env`
- contraseñas
- tokens
- claves privadas
- connection strings reales
- archivos personales que no pertenecen al producto

### No pushear sin saber en qué rama estamos

Antes de `git push`, revisar:

```bash
git status -sb
```

Ejemplo sano:

```txt
## main...origin/main
```

Eso indica que estamos en `main` y trackeamos `origin/main`.

## Glosario rápido

- **Repository**: proyecto versionado por Git.
- **Commit**: foto versionada de un conjunto de cambios.
- **Branch**: línea de trabajo independiente.
- **Remote**: repositorio externo, como GitHub.
- **origin**: nombre convencional del remoto principal.
- **HEAD**: el commit donde estamos parados ahora.
- **Working tree**: archivos reales en nuestra carpeta.
- **Staging area**: selección preparada para el próximo commit.
- **Tracked file**: archivo que Git ya sigue.
- **Untracked file**: archivo que Git ve, pero todavía no sigue.

## Flujo recomendado diario

Al empezar:

```bash
git status -sb
git fetch origin
git log --oneline --decorate -5
```

Mientras trabajamos:

```bash
git status --short
git diff
```

Antes de commitear:

```bash
git status --short
git diff --cached --stat
git diff --cached
```

Para guardar:

```bash
git add archivo-o-carpeta
git commit -m "tipo: descripcion corta"
```

Para publicar:

```bash
git push
```

## Idea central del capítulo

Git no se trata de memorizar comandos. Se trata de entender estados:

1. Qué cambió.
2. Dónde está ese cambio.
3. Si está preparado para commit.
4. Si ya está en la historia local.
5. Si ya llegó al remoto.

Si podemos responder esas cinco preguntas, Git deja de ser magia negra.
