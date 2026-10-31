"""
home_assistant_client.py

Empfohlener Weg für Butler Jaimes, Eltako-Geräte zu steuern:
Jaimes spricht NICHT direkt mit den Eltako-Aktoren, sondern nur mit
Home Assistant. Home Assistant kennt die Eltako-Geräte (klassische
EnOcean-Aktoren via FAM14/FGW14-USB + Integration "home-assistant-eltako",
oder Series-62-IP-Geräte, sobald dafür eine HA-Integration existiert).

Voraussetzung in Home Assistant:
- Long-Lived Access Token erzeugen (Profil -> Sicherheit -> Token erstellen)
- Eltako-Gerät ist als Entity registriert, z. B. light.wohnzimmer oder
  switch.kaffeemaschine

Beispiel-YAML für die klassische Eltako-Integration (Baureihe 14, EnOcean):

    eltako:
      gateway:
        - id: 1
          device_type: fam14        # oder fgw14usb
          base_id: FF-AA-80-00
          devices:
            light:
              - id: 00-00-00-02
                eep: A5-38-08
                name: Wohnzimmerlicht
            switch:
              - id: 00-00-00-05
                eep: A5-38-08
                name: Kaffeemaschine
"""

from __future__ import annotations
import requests


class HomeAssistantClient:
    def __init__(self, base_url: str, token: str, timeout: float = 5.0):
        """
        base_url: z. B. "http://<server-ip>:8123"
        token: Long-Lived Access Token aus Home Assistant
        """
        self.base_url = base_url.rstrip("/")
        self.timeout = timeout
        self._session = requests.Session()
        self._session.headers.update({
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json",
        })

    # ---------- Low-Level ----------

    def get_state(self, entity_id: str) -> dict:
        """Aktuellen Zustand einer Entity abfragen, z. B. light.wohnzimmer"""
        resp = self._session.get(
            f"{self.base_url}/api/states/{entity_id}", timeout=self.timeout
        )
        resp.raise_for_status()
        return resp.json()

    def call_service(self, domain: str, service: str, data: dict | None = None) -> list:
        """
        Generischer Service-Call, z. B.:
        call_service("light", "turn_on", {"entity_id": "light.wohnzimmer", "brightness_pct": 30})
        """
        resp = self._session.post(
            f"{self.base_url}/api/services/{domain}/{service}",
            json=data or {},
            timeout=self.timeout,
        )
        resp.raise_for_status()
        return resp.json()

    # ---------- High-Level (für Jaimes' Intent-Handler) ----------

    def light_turn_on(self, entity_id: str, brightness_pct: int | None = None) -> None:
        data = {"entity_id": entity_id}
        if brightness_pct is not None:
            data["brightness_pct"] = brightness_pct
        self.call_service("light", "turn_on", data)

    def light_turn_off(self, entity_id: str) -> None:
        self.call_service("light", "turn_off", {"entity_id": entity_id})

    def switch_turn_on(self, entity_id: str) -> None:
        self.call_service("switch", "turn_on", {"entity_id": entity_id})

    def switch_turn_off(self, entity_id: str) -> None:
        self.call_service("switch", "turn_off", {"entity_id": entity_id})

    def is_on(self, entity_id: str) -> bool:
        return self.get_state(entity_id)["state"] == "on"


# ---------- Beispielnutzung, z. B. in backend/smarthome/eltako.py ----------

if __name__ == "__main__":
    ha = HomeAssistantClient(
        base_url="http://192.168.1.50:8123",
        token="DEIN_LONG_LIVED_ACCESS_TOKEN",
    )

    # UC-04: "Jaimes, dimme das Wohnzimmerlicht auf 30%"
    try:
        ha.light_turn_on("light.wohnzimmer", brightness_pct=30)
        print("Wohnzimmerlicht auf 30% gedimmt.")
    except requests.exceptions.RequestException as exc:
        # Fällt hierhin, falls Home Assistant nicht erreichbar ist -> UC-17
        print(f"Gerät nicht erreichbar: {exc}")

    # UC-05: "Jaimes, schalte die Kaffeemaschine ein"
    ha.switch_turn_on("switch.kaffeemaschine")
    print("Status Kaffeemaschine:", "an" if ha.is_on("switch.kaffeemaschine") else "aus")
