import React, { useState, useEffect } from 'react';
import { api } from '../services/api';

interface ModelInfo {
    models: string[];
    currentModel: string;
    isLoaded: boolean;
}

const ModelManager: React.FC = () => {
    const [info, setInfo] = useState<ModelInfo>({ models: [], currentModel: '', isLoaded: false });
    const [downloadUrl, setDownloadUrl] = useState('');
    const [downloadName, setDownloadName] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [message, setMessage] = useState('');

    const fetchModels = async () => {
        try {
            const data = await api.models.list();
            setInfo(data);
        } catch (error) {
            console.error('Failed to fetch models', error);
        }
    };

    useEffect(() => {
        fetchModels();
    }, []);

    const handleDownload = async () => {
        if (!downloadUrl || !downloadName) return;
        setIsLoading(true);
        setMessage('Downloading... This may take a while.');

        try {
            const response = await fetch('http://localhost:5055/api/models/download', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ url: downloadUrl, fileName: downloadName })
            });

            if (response.ok) {
                setMessage('Download completed!');
                fetchModels();
            } else {
                setMessage('Download failed.');
            }
        } catch (error) {
            setMessage('Error downloading model.');
        } finally {
            setIsLoading(false);
        }
    };

    const handleLoad = async (fileName: string) => {
        setIsLoading(true);
        setMessage(`Loading ${fileName}...`);

        try {
            await api.models.load(fileName);
            setMessage(`Model ${fileName} loaded successfully!`);
            fetchModels();
        } catch (error) {
            setMessage('Failed to load model.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div style={{ padding: '20px', maxWidth: '800px', margin: '0 auto', color: '#fff' }}>
            <h1>Local Model Manager</h1>

            <div style={{ marginBottom: '30px', padding: '20px', backgroundColor: '#2d2d2d', borderRadius: '8px' }}>
                <h2>Current Status</h2>
                <p><strong>Status:</strong> {info.isLoaded ? 'Loaded' : 'Not Loaded'}</p>
                <p><strong>Active Model:</strong> {info.currentModel || 'None'}</p>
            </div>

            <div style={{ marginBottom: '30px', padding: '20px', backgroundColor: '#2d2d2d', borderRadius: '8px' }}>
                <h2>Download New Model</h2>
                <div style={{ display: 'flex', gap: '10px', marginBottom: '10px' }}>
                    <input
                        type="text"
                        placeholder="GGUF Download URL"
                        value={downloadUrl}
                        onChange={(e) => setDownloadUrl(e.target.value)}
                        style={{ flex: 2, padding: '8px' }}
                    />
                    <input
                        type="text"
                        placeholder="Filename (e.g., model.gguf)"
                        value={downloadName}
                        onChange={(e) => setDownloadName(e.target.value)}
                        style={{ flex: 1, padding: '8px' }}
                    />
                </div>
                <button
                    onClick={handleDownload}
                    disabled={isLoading}
                    style={{ padding: '10px 20px', backgroundColor: '#007bff', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
                >
                    {isLoading ? 'Processing...' : 'Download Model'}
                </button>
                {message && <p style={{ marginTop: '10px', color: '#aaa' }}>{message}</p>}
            </div>

            <div style={{ marginBottom: '30px', padding: '20px', backgroundColor: '#2d2d2d', borderRadius: '8px' }}>
                <h2>Load from Local Path</h2>
                <div style={{ display: 'flex', gap: '10px', marginBottom: '10px' }}>
                    <input
                        type="text"
                        placeholder="Absolute path to .gguf file (e.g. C:\Models\llama.gguf)"
                        id="localPathInput"
                        style={{ flex: 1, padding: '8px' }}
                    />
                    <button
                        onClick={() => {
                            const input = document.getElementById('localPathInput') as HTMLInputElement;
                            if (input && input.value) handleLoad(input.value);
                        }}
                        disabled={isLoading}
                        style={{ padding: '10px 20px', backgroundColor: '#17a2b8', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
                    >
                        Load Path
                    </button>
                </div>
            </div>

            <div style={{ marginBottom: '30px', padding: '20px', backgroundColor: '#2d2d2d', borderRadius: '8px' }}>
                <h2>Upload Model File</h2>
                <div style={{ display: 'flex', gap: '10px', marginBottom: '10px' }}>
                    <input
                        type="file"
                        accept=".gguf"
                        id="fileInput"
                        style={{ flex: 1, padding: '8px', color: '#fff' }}
                    />
                    <button
                        onClick={async () => {
                            const input = document.getElementById('fileInput') as HTMLInputElement;
                            if (input && input.files && input.files.length > 0) {
                                const file = input.files[0];
                                setIsLoading(true);
                                setMessage(`Uploading ${file.name}...`);

                                try {
                                    await api.models.upload(file);
                                    setMessage('Upload successful!');
                                    fetchModels();
                                } catch (error: any) {
                                    setMessage(`Upload failed: ${error.message}`);
                                } finally {
                                    setIsLoading(false);
                                }
                            }
                        }}
                        disabled={isLoading}
                        style={{ padding: '10px 20px', backgroundColor: '#ffc107', color: '#000', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
                    >
                        Upload File
                    </button>
                </div>
            </div>

            <div style={{ padding: '20px', backgroundColor: '#2d2d2d', borderRadius: '8px' }}>
                <h2>Available Models</h2>
                {info.models.length === 0 ? (
                    <p>No models found. Download one to get started.</p>
                ) : (
                    <ul style={{ listStyle: 'none', padding: 0 }}>
                        {info.models.map((modelPath, index) => {
                            const fileName = modelPath.split(/[\\/]/).pop() || modelPath;
                            return (
                                <li key={index} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '10px', borderBottom: '1px solid #444' }}>
                                    <span>{fileName}</span>
                                    <button
                                        onClick={() => handleLoad(fileName)}
                                        disabled={isLoading || info.currentModel === fileName}
                                        style={{
                                            padding: '5px 15px',
                                            backgroundColor: info.currentModel === fileName ? '#28a745' : '#6c757d',
                                            color: '#fff',
                                            border: 'none',
                                            borderRadius: '4px',
                                            cursor: info.currentModel === fileName ? 'default' : 'pointer'
                                        }}
                                    >
                                        {info.currentModel === fileName ? 'Active' : 'Load'}
                                    </button>
                                </li>
                            );
                        })}
                    </ul>
                )}
            </div>
        </div >
    );
};

export default ModelManager;
