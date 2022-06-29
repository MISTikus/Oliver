import React, { useEffect, useState } from 'react';
import logo from './logo.svg';
import './App.scss';

function App() {
  const [error, setError] = useState(false);
  const [isLoaded, setIsLoaded] = useState(false);
  const [sampleJson, setJson] = useState("");

  useEffect(() => {
    fetch("/api/v1/Variables/Some/Prod")
      .then(res => res.json())
      .then(
        (result) => {
          setIsLoaded(true);
          setJson(JSON.stringify(result, null, 2));
        },
        (error) => {
          setIsLoaded(true);
          setError(true);
        })
  }, [])


  if (error) {
    return <div>Ошибка...</div>;
  } else if (!isLoaded) {
    return <div>Загрузка...</div>;
  } else {
    return (
      <div className="App">
        <header className="App-header">
          <img src={logo} className="App-logo" alt="logo" />
          <p>
            Edit <code>src/App.tsx</code> and save to reload.
          </p>
          <a
            className="App-link"
            href="https://reactjs.org"
            target="_blank"
            rel="noopener noreferrer"
          >
            Learn React
          </a>
          <pre>
            {sampleJson}
          </pre>
        </header>
      </div>
    );
  }
}

export default App;
