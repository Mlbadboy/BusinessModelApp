# QuantumExecutive AI Dashboard Prototype

Layout, design notes, and reference HTML/CSS for QuantumExecutive AI Dashboard.

### `index.html`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>QuantumExecutive AI Dashboard</title>
    <link rel="stylesheet" href="styles.css">
</head>
<body>
    <header>
        <div class="logo-container">
            <img src="logo1.png" alt="Logo 1" class="logo logo-left">
            <h1>QuantumExecutive AI</h1>
            <img src="logo2.png" alt="Logo 2" class="logo logo-right">
        </div>
    </header>

    <main class="dashboard-container">
        <section class="performance-summary">
            <div class="tile">Revenue: $1.24M (+12%)</div>
            <div class="tile">Operational Efficiency: 92%</div>
            <div class="tile">Task Completion: 84%</div>
            <div class="tile">Customer Growth: 18%</div>
        </section>

        <section class="agent-management">
            <h2>Agent Management</h2>
            <div class="agent">
                <span class="agent-name">CEO Agent</span>
                <span class="status online">Online</span>
            </div>
            <div class="agent">
                <span class="agent-name">CFO Agent</span>
                <span class="status offline">Offline</span>
            </div>
        </section>

        <section class="ai-insights">
            <h2>Market Expansion Opportunities</h2>
            <ul>
                <li>Expand to Southeast Asia - 87% confidence</li>
                <li>Launch premium tier - projected 35% revenue lift</li>
            </ul>
        </section>

        <section class="project-status">
            <h2>Project Status</h2>
            <div class="project">
                <span>Product Launch (75% complete)</span>
            </div>
            <div class="project">
                <span>Marketing Campaign (In progress)</span>
            </div>
        </section>
    </main>

    <!-- Placeholder for logo in the bottom corner -->
    <div class="footer-logo">
        <img src="logo1.png" alt="Logo 1" class="footer-logo-img">
    </div>
</body>
</html>
```

### `styles.css`

```css
* {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
}

body {
    font-family: Arial, sans-serif;
    background-color: #121212;
    color: #e0e0e0;
    margin: 0;
}

header {
    position: relative;
    background-color: #1e1e1e;
    padding: 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

header h1 {
    color: #ffffff;
    font-size: 24px;
    margin: 0;
}

.logo-container {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
}

.logo {
    height: 40px;
    margin: 0 10px;
}

.logo-left {
    position: absolute;
    left: 10px;
}

.logo-right {
    position: absolute;
    right: 10px;
}

main {
    padding: 20px;
}

.dashboard-container {
    display: flex;
    flex-direction: column;
    gap: 30px;
}

.performance-summary {
    display: flex;
    justify-content: space-between;
    gap: 20px;
}

.tile {
    background-color: rgba(44, 44, 44, 0.7);
    padding: 15px;
    border-radius: 8px;
    text-align: center;
    width: 150px;
}

.agent-management {
    margin-top: 30px;
}

.agent {
    background-color: rgba(44, 44, 44, 0.7);
    padding: 10px;
    margin-bottom: 10px;
    border-radius: 8px;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.agent-name {
    font-weight: bold;
}

.status {
    font-size: 14px;
    padding: 5px;
    border-radius: 4px;
}

.online {
    background-color: green;
    color: white;
}

.offline {
    background-color: red;
    color: white;
}

.ai-insights ul {
    list-style: none;
    padding-left: 20px;
}

.ai-insights li {
    margin: 10px 0;
}

.project-status {
    margin-top: 30px;
}

.project {
    background-color: rgba(44, 44, 44, 0.7);
    padding: 10px;
    border-radius: 8px;
    margin-bottom: 10px;
}

.footer-logo {
    position: fixed;
    bottom: 10px;
    left: 10px;
    z-index: 10;
}

.footer-logo-img {
    width: 40px;
}
```
