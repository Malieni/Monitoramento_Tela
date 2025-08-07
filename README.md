# 🖥️ Sistema de Monitoramento de Tela - Módulo Técnico (C# WinForms)

Este projeto é um sistema de **monitoramento de tela** desenvolvido com **Windows Forms em C#**, com foco em supervisão de equipes.  
O sistema é composto por dois módulos principais:

- **Monitoramento Remoto:** instalado nos computadores dos colaboradores, responsável por capturar telas periodicamente e enviá-las via rede.
- **Monitoramento Técnico:** sistema principal deste repositório, que recebe, organiza e exibe as capturas para fins de auditoria técnica e gerencial.

---

## ⚙️ Funcionalidades do Módulo Técnico

- ✅ Receber imagens de tela de múltiplos colaboradores via rede (TCP/IP).
- ✅ Visualizar capturas em tempo real.
- ✅ Interface amigável desenvolvida em Windows Forms.
- ✅ Recebe informações de usuários conectados ao monitor técnico (nome do Usuário utilizado e IP da máquina).
- ✅ Logs de recebimento e falhas de conexão.

---

## 🧩 Tecnologias Utilizadas

- 🧠 **C# com Windows Forms (.NET Framework)**
- 🌐 **Sockets TCP/IP** 
- 🖼️ **PictureBox** para exibição das capturas de tela
- 🧪 **Timers e Threads** para atualização em tempo real

---
