.PHONY: docs-install docs docs-serve docs-serve-stop docs-serve-poll

DOCS_VENV := .venv-docs
MKDOCS := $(DOCS_VENV)/bin/mkdocs
PIP := $(DOCS_VENV)/bin/pip

docs-install:
	python3 -m venv $(DOCS_VENV)
	$(PIP) install --upgrade pip
	$(PIP) install -r requirements-docs.txt

docs:
	$(MKDOCS) build --clean --strict

docs-serve:
	$(MKDOCS) serve -a 0.0.0.0:8000

docs-serve-stop:
	pkill -f "mkdocs serve" || true

docs-serve-poll:
	@$(PIP) show watchdog >/dev/null 2>&1 || $(PIP) install watchdog
	@echo "Starting MkDocs with forced polling…"
	WATCHDOG_USE_POLLING=true $(MKDOCS) serve -a 0.0.0.0:8000 -f mkdocs.yml --watch docs --watch mkdocs.yml -v
