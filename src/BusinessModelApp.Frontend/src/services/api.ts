const API_BASE_URL = 'http://localhost:5055/api';

export const api = {
    builder: {
        build: async (goal: string) => {
            const response = await fetch(`${API_BASE_URL}/builder/build`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ goal }),
            });
            return response.json();
        }
    },
    models: {
        list: async () => {
            const response = await fetch(`${API_BASE_URL}/models`);
            return response.json();
        },
        load: async (fileName: string) => {
            const response = await fetch(`${API_BASE_URL}/models/load`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ fileName }),
            });
            if (!response.ok) throw new Error('Failed to load model');
            return response.json();
        },
        upload: async (file: File) => {
            const formData = new FormData();
            formData.append('file', file);
            const response = await fetch(`${API_BASE_URL}/models/upload`, {
                method: 'POST',
                body: formData
            });
            if (!response.ok) {
                const err = await response.json();
                throw new Error(err.Error || 'Upload failed');
            }
            return response.json();
        }
    }
};
