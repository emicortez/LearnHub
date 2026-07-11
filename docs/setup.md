# Setup — Antes de Empezar el Curso

> Este documento se completa UNA sola vez, antes del Día 1.
> Cuando termines, tenés el environment listo y LearnHub en GitHub.

---

## 1. Herramientas que necesitás

Antes de tocar código, instalá esto:

| Herramienta | Versión | Para qué |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 9.0+ | Compilar y correr los servicios |
| [Git](https://git-scm.com/downloads) | Cualquier reciente | Control de versiones local |
| [Visual Studio Code](https://code.visualstudio.com/) o [Rider](https://www.jetbrains.com/rider/) | Última | IDE |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Última | Correr PostgreSQL, Redis, RabbitMQ local |

Verificá que todo esté instalado:

```bash
dotnet --version    # debe mostrar 9.x.x
git --version       # debe mostrar git version 2.x.x
docker --version    # debe mostrar Docker version 27.x.x
```

Si alguno falla → instalalo antes de continuar.

---

## 2. Git vs GitHub — no son lo mismo

Este es el error conceptual más común, y vale la pena entenderlo antes de tocar nada.

### Git

**Git es una herramienta que corre en tu máquina.** Es un sistema de control de versiones — te permite guardar "fotos" del código en distintos momentos, volver atrás, y trabajar en paralelo sin romper nada.

```
Tu computadora
├── tu código
└── .git/          ← acá Git guarda toda la historia
    ├── commits    (fotos del código en distintos momentos)
    ├── branches   (líneas de desarrollo paralelas)
    └── index      (lo que está listo para la próxima foto)
```

Sin internet. Sin nube. Local, en tu máquina.

### GitHub

**GitHub es un servidor en la nube donde subís tu repositorio Git.** No es Git — es una plataforma que usa Git por debajo. Sirve para:

- Tener un backup de tu código en la nube
- Trabajar en equipo (varios pushean al mismo repo)
- Mostrar tu trabajo a recruiters y entrevistadores
- Correr CI/CD (GitHub Actions — semana 11)

```
Tu computadora          Internet          GitHub
┌─────────────┐                    ┌─────────────────┐
│   .git/     │  ──── push ────→   │  github.com/    │
│  (local)    │  ←─── pull ────    │  tu-usuario/    │
└─────────────┘                    │  learnhub       │
                                   └─────────────────┘
```

**La relación:** Git maneja el historial local. GitHub es el repositorio remoto donde sincronizás ese historial.

---

## 3. Conceptos clave de Git

Antes de ejecutar comandos, entendé qué hace cada área:

```
Working Directory     Staging Area (Index)     Repositorio Local (.git)
─────────────────     ────────────────────     ────────────────────────
  Tu código actual    Lo que preparaste         Las fotos guardadas
  (modificás acá)     para la próxima foto      (commits)

       │                      │                         │
       └──── git add ────────→│                         │
                              └──── git commit ────────→│
                                                        │
                                                        └── git push ──→ GitHub
```

### Los tres estados de un archivo en Git

- **Modified**: lo modificaste pero no le dijiste a Git nada todavía
- **Staged**: le dijiste a Git "esto va en la próxima foto" con `git add`
- **Committed**: la foto fue tomada, el cambio está guardado en la historia local

### ¿Por qué existe el Staging Area?

Parece un paso de más, pero tiene sentido: te permite armar commits quirúrgicos. Si modificaste 5 archivos pero solo 2 pertenecen al mismo cambio lógico, usás `git add` solo en esos 2 y hacés el commit. Los otros 3 quedan para el siguiente commit.

Un commit = un cambio lógico. No "guardé tres cosas random".

---

## 4. Crear el repositorio en GitHub

### Paso 1 — Crear una cuenta en GitHub

Si no tenés: [github.com/signup](https://github.com/signup)

### Paso 2 — Crear el repositorio

1. En GitHub, hacé click en **"New repository"** (el botón verde o el `+` arriba a la derecha)
2. Completá así:

| Campo | Valor |
|---|---|
| Repository name | `LearnHub` |
| Description | `.NET microservices platform — 18-week learning project` |
| Visibility | **Public** (los recruiters lo tienen que poder ver) |
| Initialize with README | **No** (ya tenemos código) |
| .gitignore | **None** (vamos a crear el nuestro) |
| License | **None** |

3. Click en **"Create repository"**

GitHub te va a mostrar instrucciones. No las sigas todavía — seguí esta guía.

### Paso 3 — Configurar tu identidad en Git

Git necesita saber quién sos para firmar los commits:

```bash
git config --global user.name "Tu Nombre"
git config --global user.email "tu@email.com"
```

Usá el mismo email que registraste en GitHub — así los commits aparecen vinculados a tu perfil.

Verificá:
```bash
git config --global --list
# debe mostrar user.name y user.email
```

---

## 5. Inicializar el repositorio local

Desde la carpeta raíz del proyecto (`LearnHub/`):

```bash
# Inicializar Git en el proyecto
git init

# Ver el estado inicial (todos los archivos aparecen como "untracked")
git status
```

### Crear el .gitignore

Antes de hacer cualquier commit, necesitás decirle a Git qué archivos NUNCA debe versionar. En .NET hay archivos que se generan automáticamente y no tienen que estar en el repo:

```bash
# Descargá el .gitignore estándar para .NET
# (en Windows PowerShell o desde el IDE)
dotnet new gitignore
```

Esto crea un `.gitignore` que excluye:
- `bin/` y `obj/` — binarios compilados (se regeneran con `dotnet build`)
- `.vs/` — configuración local de Visual Studio
- `*.user` — preferencias personales del IDE
- Secretos y configuraciones locales

**¿Por qué importa el .gitignore?**

Si commitiás `bin/` por error, el repo pesa 10x más de lo necesario. Si commitiás un archivo con una connection string, expusiste credenciales en internet. El `.gitignore` es la primera línea de defensa.

### Primer commit

```bash
# Agregar todos los archivos (excepto los ignorados)
git add .

# Ver qué va a entrar en el commit (nunca commitees a ciegas)
git status

# Tomar la primera "foto" del proyecto
git commit -m "chore: initial project setup"
```

**¿Por qué ese formato de mensaje?**

Los mensajes siguen [Conventional Commits](https://www.conventionalcommits.org/):

```
<tipo>: <descripción corta en minúsculas>

tipos:
  feat     → nueva feature
  fix      → arreglo de bug
  chore    → tareas de mantenimiento (setup, config, deps)
  docs     → documentación
  refactor → refactor sin cambio de comportamiento
  test     → agregar o modificar tests
```

Un buen historial de commits se lee como un log de decisiones. Un mal historial dice "arreglé cosas" en todos los commits.

---

## 6. Conectar el repo local con GitHub

```bash
# Decirle a Git que el "remote" se llama "origin" y está en GitHub
git remote add origin https://github.com/TU-USUARIO/LearnHub.git

# Renombrar la branch principal a "main" (estándar moderno)
git branch -M main

# Subir el código a GitHub por primera vez
git push -u origin main
```

El flag `-u` (upstream) conecta tu branch local `main` con `origin/main` en GitHub. Después de esto, solo necesitás `git push` (sin argumentos) para pushear.

Verificá: andá a `github.com/TU-USUARIO/LearnHub` — deberías ver tu código ahí.

---

## 7. Estrategia de branches para el curso

Durante 18 semanas vas a trabajar en distintas features. Necesitás una forma de organizar el trabajo para que la historia del repo cuente la historia del aprendizaje.

### La estrategia

```
main
  └── semana-01/identity-domain       ← trabajo de día 1-2
  └── semana-01/application-layer     ← trabajo de día 3
  └── semana-01/infrastructure-layer  ← trabajo de día 4
  └── semana-02/catalog-service       ← semana siguiente
  └── semana-03/docker-setup
```

### ¿Por qué branches y no commitear directo en main?

Porque **main representa el estado estable del proyecto**. Cuando un recruiter o entrevistador entra a tu repo y ve `main`, tiene que ver código que funciona. Las branches son el espacio de trabajo.

El flujo es:
1. Creás una branch para el feature
2. Trabajás ahí con commits frecuentes (aunque sean "work in progress")
3. Cuando está listo → merge a main
4. Borrás la branch

### Comandos que vas a usar todos los días

```bash
# Ver en qué branch estás y el estado de los archivos
git status

# Crear una branch nueva y moverse a ella
git checkout -b semana-01/identity-domain

# Ver todas las branches
git branch

# Cambiar de branch (sin crear)
git checkout main

# Agregar archivos al staging
git add src/Services/Identity/Identity.Domain/

# Hacer el commit
git commit -m "feat(identity): add User aggregate root with factory method"

# Pushear la branch a GitHub
git push origin semana-01/identity-domain

# Traer cambios del remote (si trabajás en más de una máquina)
git pull
```

### Pull Request — el flujo completo

Cuando terminás una feature:

1. Pusheás la branch a GitHub (`git push origin tu-branch`)
2. En GitHub, aparece un botón "Compare & pull request"
3. Abrís el Pull Request: título claro, descripción de qué hiciste
4. Lo revisás vos mismo (en el curso no hay equipo, pero el hábito importa)
5. Merge a main
6. Borrás la branch

**¿Por qué Pull Requests aunque seas el único developer?**

Porque en una entrevista te van a preguntar cómo trabajás en equipo. Y porque el PR queda como documentación: cada feature tiene su PR con descripción, los commits agrupados, y el diff completo. Es parte del portfolio.

---

## 8. Arrancar el stack por primera vez

Con el repo listo, verificá que el proyecto compila:

```bash
# Desde la raíz del proyecto
dotnet build LearnHub.sln
```

Debería compilar sin errores. Si falla → revisá que el .NET SDK 9.x esté instalado.

Para correr el stack completo (cuando llegues a tener servicios funcionando):

```bash
dotnet run --project src/AppHost/LearnHub.AppHost
```

Esto levanta el Aspire Dashboard en `http://localhost:15888` — desde ahí vas a ver todos los servicios, sus logs, y sus health checks.

---

## 9. Estado final — checklist

Antes de arrancar el Día 1, verificá que tenés todo:

- [ ] .NET SDK 9.x instalado (`dotnet --version`)
- [ ] Git instalado y configurado con tu nombre y email
- [ ] Docker Desktop corriendo
- [ ] Repositorio `LearnHub` creado en GitHub (público)
- [ ] Código subido a `main` en GitHub
- [ ] `dotnet build LearnHub.sln` pasa sin errores
- [ ] Sabés crear una branch, hacer un commit, y pushear

Si todo está tildado → estás listo para el Día 1.

---

## Referencia rápida — comandos de Git

```bash
# Estado y diferencias
git status                    # qué cambió
git diff                      # diff de lo no staged
git diff --staged             # diff de lo staged
git log --oneline --graph     # historia visual del repo

# Branches
git checkout -b nombre        # crear y moverse a branch nueva
git checkout nombre           # moverse a branch existente
git branch -d nombre          # borrar branch local (después del merge)
git branch -a                 # ver todas las branches (local + remote)

# Cambios
git add archivo               # stagear un archivo específico
git add .                     # stagear todo
git commit -m "mensaje"       # commitear
git push origin rama          # pushear al remote

# Sync
git pull                      # traer cambios del remote y hacer merge
git fetch                     # traer cambios del remote sin merge
```
