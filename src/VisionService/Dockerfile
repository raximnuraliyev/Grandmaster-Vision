FROM python:3.11-slim

WORKDIR /app

# Copy requirements and install (using headless opencv - no GL needed)
COPY src/VisionService/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application
COPY src/VisionService/main.py .

EXPOSE 8000

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
